using System.Windows.Forms;

namespace EasyTabs.Model;

/// <summary>
/// FormEventArgs class.
/// </summary>
public class FormEventArgs : System.EventArgs
{
    /// <summary>
    /// Creates a FormEventArgs object.
    /// </summary>
    /// <param name="value">The initial form.</param>
    public FormEventArgs(Form? value)
    {
        Form = value;
    }

    /// <summary>
    /// Creates a FormEventArgs object.
    /// </summary>
    public FormEventArgs()
    {
    }

    /// <summary>
    /// Gets or sets the form value.
    /// </summary>
    public Form? Form { get; set; }
}