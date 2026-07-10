$root = 'd:\Project\QTTabBar-Next\QTTabBar'
Get-ChildItem -Path $root -Recurse -Filter *.cs |
    Where-Object { $_.FullName -notmatch '\\obj\\|\\bin\\' } |
    ForEach-Object {
        $lc = (Get-Content -LiteralPath $_.FullName).Count
        [PSCustomObject]@{ Lines = $lc; Path = $_.FullName.Replace('d:\Project\QTTabBar-Next\','') }
    } |
    Sort-Object Lines -Descending |
    Select-Object -First 35 |
    Format-Table -AutoSize
