# Talos


```bash
kubectl get secret vivabm-data-secret -n vivabm-data -o jsonpath='{.data.POSTGRES_PASSWORD}' | % { [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($_)) }

kubectl get secret vivabm-data-secret -n vivabm-data -o jsonpath='{.data.BUDDHAM_PASSWORD}' | % { [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($_))

kubectl get secret vivabm-data-secret -n vivabm-data -o jsonpath='{.data.BUDDHAM_MASTER_PASSWORD}' | % { [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($_)) }

kubectl get secret vivabm-data-secret -n vivabm-data -o jsonpath='{.data.KIMBUMJUN_MASTER_PASSWORD}' | % { [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($_)) }
kubectl get secret vivabm-data-secret -n vivabm-data -o jsonpath='{.data.VIVABM_MASTER_PASSWORD}' | % { [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($_)) }
kubectl get secret vivabm-data-secret -n vivabm-data -o jsonpath='{.data.VIVAKR_MASTER_PASSWORD}' | % { [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($_)) }
kubectl get secret vivabm-data-secret -n vivabm-data -o jsonpath='{.data.WRITER_MASTER_PASSWORD}' | % { [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($_)) }
kubectl get secret vivabm-data-secret -n vivabm-data -o jsonpath='{.data.TEXT_MASTER_PASSWORD}' | % { [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($_)) }


kubectl create secret generic buddham-db-secret -n buddham `
  --from-literal=BUDDHAM_MASTER_PASSWORD='Z4920!7365#' `
  --dry-run=client -o yaml | kubectl apply -f -

kubectl rollout restart deployment/buddham-api-deploy -n buddham

```
