namespace Jalium.UI.Controls.Editor;

/// <summary>
/// Classification of a syntax token for highlighting.
/// </summary>
public enum TokenClassification
{
    PlainText,
    Keyword,
    ControlKeyword,
    TypeName,
    StructName,
    EnumName,
    InterfaceName,
    DelegateName,
    String,
    Character,
    Number,
    Comment,
    XmlDoc,
    Preprocessor,
    Operator,
    Punctuation,
    Identifier,
    LocalVariable,
    Parameter,
    Field,
    EnumMember,
    Property,
    Method,
    Namespace,
    Attribute,
    BindingKeyword,
    BindingParameter,
    BindingPath,
    BindingOperator,
    Error
}
