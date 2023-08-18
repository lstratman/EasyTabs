using NUnit.Framework;

namespace EasyTabsTests;

[TestFixture]
public class HostFormTests
{


    [Test]
    public async Task Form1_AddFormFromOtherThread()
    {
        await new Func<Task>(
            () =>
            {
                var mainForm = new HostForm();
                mainForm.Load += async (_, _) =>
                {
                    await DoWork(mainForm);
                };
                Application.Run(mainForm);
                return TaskEx.CompletedTask;
            }).RunWithCompletionSource(false, true);
    }

    private static async Task DoWork(HostForm mainForm)
    {
        await mainForm.Button1OnClick();
        await TimeSpan.FromSeconds(1).Delay();
        await mainForm.Button2OnClick();
        await TimeSpan.FromSeconds(1).Delay();
        mainForm.Close();
    }
}