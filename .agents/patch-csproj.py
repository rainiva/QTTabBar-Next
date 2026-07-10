import pathlib
p = pathlib.Path(r"D:\Project\QTTabBar-Next\QTTabBar\QTTabBar.csproj")
text = p.read_text(encoding="utf-8")
needle = """    <Compile Include=\"QTDesktopTool.TooltipController.cs\">
      <DependentUpon>QTDesktopTool.cs</DependentUpon>
    </Compile>"""
insert = needle + """
    <Compile Include=\"QTDesktopTool.HookController.cs\">
      <DependentUpon>QTDesktopTool.cs</DependentUpon>
    </Compile>
    <Compile Include=\"QTDesktopTool.SettingsController.cs\">
      <DependentUpon>QTDesktopTool.cs</DependentUpon>
    </Compile>"""
if "QTDesktopTool.HookController.cs" in text:
    print("Already present")
else:
    if needle not in text:
        raise SystemExit("needle not found")
    p.write_text(text.replace(needle, insert), encoding="utf-8", newline="\r\n")
    print("Updated csproj")
