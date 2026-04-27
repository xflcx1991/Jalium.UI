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

dam = '[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] '

# Match Convert(... ) or ConvertBack(... ) signature with `Type targetType` as a single param (not Type[]).
# Using a permissive pattern: 'Type targetType,' inside parentheses where the start is `Convert(` or `ConvertBack(`.
pat = re.compile(r'((?:Convert|ConvertBack)\([^)]*?)\bType targetType\b')

for fp in files:
    p = pathlib.Path(fp)
    text = p.read_text(encoding='utf-8')
    orig = text
    out = []
    last = 0
    for m in pat.finditer(text):
        head = m.group(1)
        if 'DynamicallyAccessedMembers' in head:
            # already annotated for this signature — leave as is
            continue
        out.append(text[last:m.start()])
        out.append(head + dam + 'Type targetType')
        last = m.end()
    out.append(text[last:])
    new_text = ''.join(out)
    if new_text != orig:
        p.write_text(new_text, encoding='utf-8')
        print(f'updated {fp}')
    else:
        print(f'no change {fp}')
