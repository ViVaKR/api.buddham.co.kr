# BuddhamDeploy.ps1
# Functions 폴더에 두고 프로파일에서 닷 소싱하는 파일입니다. (함수 정의만 있고, 실행 코드는 없음)
# 솔루션 루트(Buddham.Api/, k8s/ 가 있는 폴더)에서 실행하거나 -Root 로 지정하세요.
#
#   Get-BuddhamAppMap        앱 3개의 Dockerfile / 매니페스트 / Deployment 이름 (데이터)
#   Get-BuddhamImageTag      배포마다 고유한 태그 (시각-gitSHA[-dirty])
#   Test-BuddhamDeployPrereq docker / ssh / kubectl 사전 점검
#   Build-BuddhamImage       맥에서 linux/arm64 이미지를 병렬 빌드, 하나라도 실패하면 중단
#   Send-BuddhamImage        이미지를 tar 로 묶어 전송하고 k3s 에 import (sudo 암호 1회)
#   Publish-BuddhamApp       매니페스트의 image 태그만 바꿔 apply → rollout 확인 → 실패 시 자동 롤백
#   Start-BuddhamDeploy      위 전부를 순서대로 (ai → api → chat)
#
# 모든 단계에 -WhatIf 를 쓸 수 있습니다.
#   Start-BuddhamDeploy -WhatIf
#   Start-BuddhamDeploy -App api

function Get-BuddhamAppMap {
    [ordered]@{
        ai   = @{ Dockerfile = 'Buddham.Ai/Dockerfile';   Yaml = 'k8s/application/buddham-ai.yaml';   Deploy = 'buddham-ai-deploy'   }
        api  = @{ Dockerfile = 'Buddham.Api/Dockerfile';  Yaml = 'k8s/application/buddham-api.yaml';  Deploy = 'buddham-api-deploy'  }
        chat = @{ Dockerfile = 'Buddham.Chat/Dockerfile'; Yaml = 'k8s/application/buddham-chat.yaml'; Deploy = 'buddham-chat-deploy' }
    }
}

function Get-BuddhamImageTag {
    [CmdletBinding()]
    param([string]$Root = (Get-Location).Path)

    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $sha = git -C $Root rev-parse --short HEAD 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $sha) { return $stamp }     # git 저장소가 아니면 시각만
    $dirty = git -C $Root status --porcelain 2>$null
    if ($dirty) { "$stamp-$sha-dirty" } else { "$stamp-$sha" }
}

function Test-BuddhamDeployPrereq {
    [CmdletBinding()]
    param(
        [string]$Server    = 'Ubuntu-server',
        [string]$Namespace = 'buddham'
    )
    $checks = [ordered]@{
        'docker 실행'      = { docker info *> $null; $LASTEXITCODE -eq 0 }
        'SSH 접속'         = { ssh -o BatchMode=yes -o ConnectTimeout=5 $Server true *> $null; $LASTEXITCODE -eq 0 }
        'kubectl 클러스터' = { kubectl get ns $Namespace *> $null; $LASTEXITCODE -eq 0 }
    }
    $bad = foreach ($k in $checks.Keys) { if (-not (& $checks[$k])) { $k } }
    if ($bad) { throw "사전 점검 실패: $($bad -join ', ')" }
}

function Build-BuddhamImage {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [ValidateSet('ai', 'api', 'chat')][string[]]$App = @('ai', 'api', 'chat'),
        [string]$Tag,
        [string]$Root     = (Get-Location).Path,
        [string]$Platform = 'linux/arm64'
    )
    if (-not $Tag) { $Tag = Get-BuddhamImageTag -Root $Root }
    $map = Get-BuddhamAppMap

    $jobs = foreach ($a in ($map.Keys | Where-Object { $_ -in $App })) {
        [pscustomobject]@{ App = $a; Image = "buddham-${a}:$Tag"; Dockerfile = $map[$a].Dockerfile }
    }
    $todo = @($jobs | Where-Object { $PSCmdlet.ShouldProcess($_.Image, "docker build --platform $Platform") })
    if ($WhatIfPreference) { return $jobs | Select-Object App, Image }   # 다음 단계의 -WhatIf 가 이어지도록
    if ($todo.Count -eq 0) { return }

    # 병렬 실행 안에서는 $ErrorActionPreference 등이 상속되지 않으므로, 종료 코드를 객체로 모아 직접 판정
    $results = $todo | ForEach-Object -Parallel {
        $sw  = [Diagnostics.Stopwatch]::StartNew()
        $log = Join-Path ([IO.Path]::GetTempPath()) "buddham-build-$($_.App).log"
        $df  = Join-Path $using:Root $_.Dockerfile
        docker build --progress=plain --platform $using:Platform -t $_.Image -f $df $using:Root *> $log
        [pscustomobject]@{
            App      = $_.App
            Image    = $_.Image
            ExitCode = $LASTEXITCODE
            Seconds  = [math]::Round($sw.Elapsed.TotalSeconds, 1)
            Log      = $log
        }
    } -ThrottleLimit 3

    $results | Format-Table App, Image, ExitCode, Seconds -AutoSize | Out-Host

    $failed = @($results | Where-Object ExitCode -ne 0)
    if ($failed.Count -gt 0) {
        foreach ($f in $failed) {
            Write-Warning "[$($f.App)] 빌드 로그: $($f.Log)  (마지막 20줄)"
            Get-Content $f.Log -Tail 20 | Out-Host
        }
        throw "빌드 실패: $($failed.App -join ', ')"
    }
    $results | Select-Object App, Image
}

function Send-BuddhamImage {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)][string[]]$Image,
        [string]$Server    = 'Ubuntu-server',
        [string]$RemoteTar = '/tmp/buddham-images.tar'
    )
    begin   { $all = [System.Collections.Generic.List[string]]::new() }
    process { $all.AddRange([string[]]$Image) }
    end {
        if ($all.Count -eq 0) { return }
        if (-not $PSCmdlet.ShouldProcess($Server, "전송 및 k3s import: $($all -join ', ')")) { return }

        foreach ($i in $all) {
            docker image inspect $i *> $null
            if ($LASTEXITCODE -ne 0) { throw "로컬에 이미지가 없습니다: $i" }
        }

        $tmp = Join-Path ([IO.Path]::GetTempPath()) 'buddham-images.tar'
        try {
            docker save -o $tmp @all
            if ($LASTEXITCODE -ne 0) { throw 'docker save 실패' }

            scp -C $tmp "${Server}:${RemoteTar}"
            if ($LASTEXITCODE -ne 0) { throw 'scp 전송 실패' }

            # import 만 -t 로 실행: 여기서 sudo 암호를 한 번 묻습니다. 끝나면 tar 삭제.
            $remote = "sudo k3s ctr images import $RemoteTar; rc=`$?; rm -f $RemoteTar; exit `$rc"
            ssh -t $Server $remote
            if ($LASTEXITCODE -ne 0) { throw 'k3s import 실패' }
        }
        finally { Remove-Item $tmp -Force -ErrorAction SilentlyContinue }
    }
}

function Publish-BuddhamApp {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [ValidateSet('ai', 'api', 'chat')][string[]]$App = @('ai', 'api', 'chat'),
        [Parameter(Mandatory)][string]$Tag,
        [string]$Root           = (Get-Location).Path,
        [string]$Namespace      = 'buddham',
        [int]$TimeoutSeconds    = 180
    )
    $map = Get-BuddhamAppMap

    foreach ($a in ($map.Keys | Where-Object { $_ -in $App })) {
        $m = $map[$a]
        $yamlPath = Join-Path $Root $m.Yaml
        if (-not (Test-Path $yamlPath)) { throw "매니페스트가 없습니다: $yamlPath" }

        # 파일은 건드리지 않고, 메모리에서 image 태그만 바꿔 apply (YAML 은 net10 으로 남겨 둠)
        $raw     = Get-Content $yamlPath -Raw
        $pattern = "(image:\s+)buddham-${a}:\S+"
        if ($raw -notmatch $pattern) { throw "[$a] 'image: buddham-${a}:...' 줄을 찾지 못했습니다: $yamlPath" }
        $newImage = "buddham-${a}:$Tag"
        $yaml     = $raw -replace $pattern, ('${1}' + $newImage)

        if (-not $PSCmdlet.ShouldProcess("$($m.Deploy) → $newImage", 'kubectl apply + rollout status')) { continue }

        $yaml | kubectl apply -n $Namespace -f -
        if ($LASTEXITCODE -ne 0) { throw "[$a] kubectl apply 실패" }

        kubectl rollout status "deployment/$($m.Deploy)" -n $Namespace --timeout="${TimeoutSeconds}s"
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "[$a] 롤아웃 실패 → 이전 버전으로 롤백합니다"
            kubectl rollout undo "deployment/$($m.Deploy)" -n $Namespace
            kubectl rollout status "deployment/$($m.Deploy)" -n $Namespace --timeout=120s
            throw "[$a] 배포 실패 (이전 버전으로 롤백됨). 확인: kubectl logs deploy/$($m.Deploy) -n $Namespace"
        }
        [pscustomobject]@{ App = $a; Image = $newImage; Status = 'Rolled out' }
    }
}

function Start-BuddhamDeploy {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [ValidateSet('ai', 'api', 'chat')][string[]]$App = @('ai', 'api', 'chat'),
        [string]$Root   = (Get-Location).Path,
        [string]$Server = 'Ubuntu-server'
    )
    # -WhatIf 는 $WhatIfPreference 로 아래 세 함수에 자동 전달됩니다.
    if (-not $WhatIfPreference) { Test-BuddhamDeployPrereq -Server $Server }

    $tag = Get-BuddhamImageTag -Root $Root
    Write-Host "== 태그: $tag ==" -ForegroundColor Cyan

    Build-BuddhamImage -App $App -Tag $tag -Root $Root | Send-BuddhamImage -Server $Server
    Publish-BuddhamApp -App $App -Tag $tag -Root $Root
}
