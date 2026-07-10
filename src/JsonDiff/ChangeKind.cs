namespace JsonDiff;

/// <summary>
/// The kind of structural change detected at a given JSON path.
/// </summary>
public enum ChangeKind
{
    /// <summary>A value/property present in the right document but missing on the left.</summary>
    Added,

    /// <summary>A value/property present in the left document but missing on the right.</summary>
    Removed,

    /// <summary>A value present in both documents whose content differs.</summary>
    Changed
}
