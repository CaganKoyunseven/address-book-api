using System.ComponentModel.DataAnnotations;

namespace AddressBook.Api.Validation;

/// <summary>
/// DataAnnotations validate a property's own value but not the elements inside a list, so a
/// plain [MaxLength] on a List&lt;string&gt; wouldn't check each tag's length. This does both:
/// at most 10 tags, each 1-30 characters after trimming.
/// </summary>
public sealed class TagListAttribute : ValidationAttribute
{
    public TagListAttribute()
    {
        ErrorMessage = "Tags must be at most 10 items, each 1-30 characters long.";
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
        {
            return true;
        }

        if (value is not IEnumerable<string?> tags)
        {
            return false;
        }

        var list = tags.ToList();
        return list.Count <= 10 && list.All(tag => tag is not null && tag.Trim().Length is >= 1 and <= 30);
    }
}
