using Ara.Core;
using Xunit;

namespace Ara.Tests
{
    public class KioskPolicyTests
    {
        [Fact]
        public void OwnProcessIsAlwaysAllowed()
        {
            var evaluation = KioskPolicy.Evaluate(
                AssessmentMode.Exam,
                "ARA Assessment.exe",
                "WindowsForms10.Window.8.app.0.141b42a_r6_ad1",
                "ARA Assessment",
                true);

            Assert.True(evaluation.Allowed);
        }

        [Fact]
        public void NormalModeAllowsEveryApplication()
        {
            var evaluation = KioskPolicy.Evaluate(
                AssessmentMode.Normal,
                "chrome.exe",
                "Chrome_WidgetWin_1",
                "Google Chrome",
                false);

            Assert.True(evaluation.Allowed);
        }

        [Theory]
        [InlineData("winword.exe", "OpusApp", "Document1 - Microsoft Word")]
        [InlineData("excel.exe", "XLMAIN", "Book1 - Microsoft Excel")]
        [InlineData("powerpnt.exe", "PPTFrameClass", "Presentation1 - Microsoft PowerPoint")]
        [InlineData("wps.exe", "OpusApp", "Document1 - WPS Writer")]
        [InlineData("soffice.exe", "SALFRAME", "LibreOffice Writer")]
        public void ExamModeAllowsOfficeApplications(string exe, string className, string title)
        {
            var evaluation = KioskPolicy.Evaluate(
                AssessmentMode.Exam,
                exe,
                className,
                title,
                false);

            Assert.True(evaluation.Allowed);
        }

        [Theory]
        [InlineData("chrome.exe", "Chrome_WidgetWin_1", "Google Chrome")]
        [InlineData("msedge.exe", "Chrome_WidgetWin_1", "Microsoft Edge")]
        [InlineData("notepad.exe", "Notepad", "Untitled - Notepad")]
        [InlineData("telegram.exe", "TLiveForm", "Telegram")]
        public void ExamModeBlocksUnauthorizedApplications(string exe, string className, string title)
        {
            var evaluation = KioskPolicy.Evaluate(
                AssessmentMode.Exam,
                exe,
                className,
                title,
                false);

            Assert.False(evaluation.Allowed);
        }

        [Fact]
        public void ExamModeBlocksFileExplorer()
        {
            var evaluation = KioskPolicy.Evaluate(
                AssessmentMode.Exam,
                "explorer.exe",
                "CabinetWClass",
                "This PC",
                false);

            Assert.False(evaluation.Allowed);
        }

        [Fact]
        public void ExamModeAllowsTaskbarAndDesktopShellClasses()
        {
            var taskbar = KioskPolicy.Evaluate(
                AssessmentMode.Exam,
                string.Empty,
                "Shell_TrayWnd",
                string.Empty,
                false);

            var desktop = KioskPolicy.Evaluate(
                AssessmentMode.Exam,
                "explorer.exe",
                "WorkerW",
                string.Empty,
                false);

            Assert.True(taskbar.Allowed);
            Assert.True(desktop.Allowed);
        }

        [Fact]
        public void ExamModeBlocksRunDialog()
        {
            var evaluation = KioskPolicy.Evaluate(
                AssessmentMode.Exam,
                "explorer.exe",
                "#32770",
                "Run",
                false);

            Assert.False(evaluation.Allowed);
        }

        [Fact]
        public void ExamModeBlocksElevatedProcessWithEmptyExecutable()
        {
            var evaluation = KioskPolicy.Evaluate(
                AssessmentMode.Exam,
                string.Empty,
                "MMCMainFrame",
                "Device Manager",
                false);

            Assert.False(evaluation.Allowed);
        }

        [Fact]
        public void ExamModeAllowsTaskManagerAsEmergencyPath()
        {
            var evaluation = KioskPolicy.Evaluate(
                AssessmentMode.Exam,
                "taskmgr.exe",
                "TaskManagerWindow",
                "Windows Task Manager",
                false);

            Assert.True(evaluation.Allowed);
            Assert.True(evaluation.IsEmergency);
        }

        [Fact]
        public void ExamModeAllowsWindowsShellProcesses()
        {
            var evaluation = KioskPolicy.Evaluate(
                AssessmentMode.Exam,
                "textinputhost.exe",
                "Windows.UI.Core.CoreWindow",
                string.Empty,
                false);

            Assert.True(evaluation.Allowed);
        }
    }
}
