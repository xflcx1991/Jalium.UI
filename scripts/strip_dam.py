import re
import pathlib

files = [
    'src/managed/Jalium.UI.Core/Data/ValueConverters.cs',
    'src/managed/Jalium.UI.Xaml/RazorBinding.cs',
    'src/managed/Jalium.UI.Controls/SplashScreen.cs',
    'src/managed/Jalium.UI.Controls/MenuScrollingVisibilityConverter.cs',
    'tests/Jalium.UI.Tests/MultiBindingWpfParityTests.cs',
    'tests/Jalium.UI.Tests/BindingTests.cs',
]

dam_pattern = re.compile(
    r'\[System\.Diagnostics\.CodeAnalysis\.DynamicallyAccessedMembers\(System\.Diagnostics\.CodeAnalysis\.DynamicallyAccessedMemberTypes\.PublicParameterlessConstructor\)\]\s+'
)

for fp in files:
    p = pathlib.Path(fp)
    text = p.read_text(encoding='utf-8')
    new_text = dam_pattern.sub('', text)
    if new_text != text:
        p.write_text(new_text, encoding='utf-8')
        print(f'updated {fp}')
    else:
        print(f'no change {fp}')
