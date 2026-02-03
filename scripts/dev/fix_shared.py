import xml.etree.ElementTree as ET

csproj_path = r"C:\lunar-horse\yokan-projects\fantasim-shared\project\contracts\FantaSim.Shared\FantaSim.Shared.csproj"

# Parse the csproj file
tree = ET.parse(csproj_path)
root = tree.getroot()

# Register the namespace
ET.register_namespace("", "http://schemas.microsoft.com/developer/msbuild/2003")

# Find the ItemGroup with ProjectReferences
ns = {"": "http://schemas.microsoft.com/developer/msbuild/2003"}
item_groups = root.findall(".//ItemGroup", ns)
item_group = item_groups[1] if len(item_groups) > 1 else item_groups[0]

# Check if AutoToString generator is already referenced
existing_refs = [ref.get("Include") for ref in item_group.findall("ProjectReference", ns)]

# Add AutoToString generator if not present
auto_to_string_gen = r"..\..\..\..\..\plate-projects\plate-shared\dotnet\source-generators\General.AutoToString.Set\General.AutoToString\General.AutoToString.csproj"
if not any(
    "General.AutoToString.csproj" in ref and "Attributes" not in ref for ref in existing_refs
):
    new_ref = ET.Element("ProjectReference")
    new_ref.set("Include", auto_to_string_gen)
    new_ref.set("OutputItemType", "Analyzer")
    new_ref.set("ReferenceOutputAssembly", "false")
    item_group.append(new_ref)
    print("Added AutoToString generator reference")

# Add DI.ConstructorInjection Attributes if not present
di_attrs = r"..\..\..\..\..\plate-projects\plate-shared\dotnet\source-generators\DI.ConstructorInjection.Set\DI.ConstructorInjection.Attributes\DI.ConstructorInjection.Attributes.csproj"
if not any("DI.ConstructorInjection.Attributes" in ref for ref in existing_refs):
    new_ref = ET.Element("ProjectReference")
    new_ref.set("Include", di_attrs)
    item_group.append(new_ref)
    print("Added DI.ConstructorInjection.Attributes reference")

# Add DI.ConstructorInjection generator if not present
di_gen = r"..\..\..\..\..\plate-projects\plate-shared\dotnet\source-generators\DI.ConstructorInjection.Set\DI.ConstructorInjection\DI.ConstructorInjection.csproj"
if not any(
    "DI.ConstructorInjection.csproj" in ref and "Attributes" not in ref for ref in existing_refs
):
    new_ref = ET.Element("ProjectReference")
    new_ref.set("Include", di_gen)
    new_ref.set("OutputItemType", "Analyzer")
    new_ref.set("ReferenceOutputAssembly", "false")
    item_group.append(new_ref)
    print("Added DI.ConstructorInjection generator reference")

# Write back
tree.write(csproj_path, xml_declaration=True, encoding="utf-8")
print(f"Updated {csproj_path}")
