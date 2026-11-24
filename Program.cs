namespace NexusCopia;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        try { Application.Run(new MainMenuForm()); }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "SQL Server requerido", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
