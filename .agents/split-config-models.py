# -*- coding: utf-8 -*-
import pathlib

root = pathlib.Path(r"D:\Project\QTTabBar-Next\QTTabBar")
config_path = root / "Config.cs"
lines = config_path.read_text(encoding="utf-8").splitlines(keepends=True)

header = "".join(lines[:38])  # through namespace opening
models_prefix = lines[38:222]  # XmlSerializableFont + enums (before Config class)
config_class_start = lines[222:332]  # [Serializable] public partial class Config { ... ctor }
nested = lines[333:1131]  # nested classes inside Config

models_header = header.replace(
    "namespace QTTabBarLib {\n\n",
    "namespace QTTabBarLib {\n"
)
models_content = models_header + "".join(models_prefix)
models_content += "\n    [Serializable]\n    public partial class Config {\n"
models_content += "".join(nested)
models_content += "    }\n}\n"

config_content = header
config_content += "    [Serializable]\n    public partial class Config {\n"
config_content += "".join(config_class_start[2:])  # skip duplicate [Serializable] and class line from original
# config_class_start[0] is [Serializable], [1] is public class Config {
config_content += "    }\n}\n"

(root / "ConfigModels.cs").write_text(models_content, encoding="utf-8")
config_path.write_text(config_content, encoding="utf-8")
print("Config.cs lines:", len(config_content.splitlines()))
print("ConfigModels.cs lines:", len(models_content.splitlines()))
