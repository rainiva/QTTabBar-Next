Get-ChildItem ''d:\Project\QTTabBar-Next\QTTabBar\*.cs'' -Recurse | ForEach-Object {
     = (Get-Content .FullName | Measure-Object -Line).Lines
    [PSCustomObject]@{Lines=; File=.FullName.Replace('d:\Project\QTTabBar-Next\','')}
} | Sort-Object Lines -Descending | Select-Object -First 25 | Format-Table -AutoSize
