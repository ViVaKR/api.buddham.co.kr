$args = @("create", "secret", "generic", "buddham-app-secret", "-n", "buddham")

dotnet user-secrets list | ForEach-Object {
  $parts = $_ -split ' = ', 2
  if ($parts.Count -eq 2) {
    # ':' → '__' 로, '.' → '-' 로 변환 (Secret 키 규칙에 맞추기)
    $key = $parts[0].Trim().Replace(':', '__').Replace('.', '-')
    $value = $parts[1].Trim()
    $args += "--from-literal=$key=$value"
  }
}

kubectl @args
