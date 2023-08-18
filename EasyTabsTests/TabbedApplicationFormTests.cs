using System.Windows.Documents;
using CoreLibrary.Extensions.WaitUtility;
using EasyTabs;
using NUnit.Framework;

namespace EasyTabsTests;

[TestFixture]
public class TabbedApplicationFormTests
{


    [Test]
    public async Task TabbedApplicationForm_UsesTabbedApplication()
    {
        await new Func<Task>(
            () =>
            {
                Func<TabbedApplicationForm> initialForm = () =>
                {
                    var tabbedApplicationForm = new TabbedApplicationForm
                    {
                        Text = "Test Form"
                    };

                    tabbedApplicationForm.Load += new OnLoadContainer(this, tabbedApplicationForm).OnTabbedApplicationFormOnLoad;
                    return tabbedApplicationForm;
                };
                _createTabbedApplication = initialForm.CreateTabbedApplication(()=> new TabbedApplicationForm
                                                                                    {
                                                                                        Text = "Next Form"
                                                                                    });
                Application.Run(_createTabbedApplication);
                return TaskEx.CompletedTask;
            }).RunWithCompletionSource(false, true);
    }

    private class OnLoadContainer
    {
        private readonly TabbedApplicationFormTests _tabbedApplicationFormTests;
        private readonly TabbedApplicationForm _tabbedApplicationForm;

        public OnLoadContainer(TabbedApplicationFormTests tabbedApplicationFormTests, TabbedApplicationForm tabbedApplicationForm)
        {
            _tabbedApplicationFormTests = tabbedApplicationFormTests;
            _tabbedApplicationForm = tabbedApplicationForm;
        }

        public void OnTabbedApplicationFormOnLoad(object s, EventArgs _)
        {
            _tabbedApplicationForm.Load -= OnTabbedApplicationFormOnLoad;
            var t = _tabbedApplicationFormTests.DoWork(_tabbedApplicationForm);
        }
    }

    private bool _called;
    private ApplicationContext _createTabbedApplication;

    private async Task DoWork(TabbedApplicationForm mainForm)
    {
        if (_called)
        {
            return;
        }

        _called = true;
        await TimeSpan.FromSeconds(1).Delay();
        Form form = await mainForm.Button1OnClick();
        await TimeSpan.FromSeconds(1).Delay();
        var formLocal = form;
        _ = new Action(
            () =>
            {
                formLocal.Invoke(
                    () =>
                    {
                        formLocal.Close();
                    });
            }).Run();
        form = await mainForm.Button2OnClick();
        await TimeSpan.FromSeconds(1).Delay();
        _ = new Action(
            () =>
            {
                form.Invoke(
                    () =>
                    {
                        form.Close();
                    });
            }).Run();
        mainForm.Dispose();
    }
}