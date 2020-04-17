using System.Windows.Controls;

namespace ZeroLevel.WPF.Controls
{
    public static class ControlUnlocker
    {
        public static void UnlockControl(Control control)
        {
            if (control != null)
            {
                if (control.Parent != null)
                {
                    if (control.Parent is Panel)
                    {
                        var parent = (System.Windows.Controls.Panel)control.Parent;
                        parent.Children.Remove(control);
                    }
                    else if (control.Parent is ContentControl)
                    {
                        var parent = (ContentControl)control.Parent;
                        parent.Content = null;
                    }
                }
            }
        }
    }
}
