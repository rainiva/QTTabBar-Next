$root = 'd:\Project\QTTabBar-Next\QTTabBar'
Write-Output '=== QTTabBarClass partials ==='
Get-ChildItem -Path $root -Filter 'QTTabBarClass*.cs' |
    ForEach-Object { [PSCustomObject]@{ Lines = (Get-Content -LiteralPath $_.FullName).Count; Name = $_.Name } } |
    Sort-Object Lines -Descending | Format-Table -AutoSize
Write-Output '=== QTUtility family ==='
Get-ChildItem -Path $root -Filter 'QTUtility*.cs' |
    ForEach-Object { [PSCustomObject]@{ Lines = (Get-Content -LiteralPath $_.FullName).Count; Name = $_.Name } } |
    Sort-Object Lines -Descending | Format-Table -AutoSize
Write-Output '=== QTButtonBar family ==='
Get-ChildItem -Path $root -Filter 'QTButtonBar*.cs' |
    ForEach-Object { [PSCustomObject]@{ Lines = (Get-Content -LiteralPath $_.FullName).Count; Name = $_.Name } } |
    Sort-Object Lines -Descending | Format-Table -AutoSize
