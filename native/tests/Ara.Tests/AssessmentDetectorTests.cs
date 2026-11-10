using Ara.Core;
using Xunit;

namespace Ara.Tests
{
    public class AssessmentDetectorTests
    {
        private readonly AssessmentDetector _detector = new AssessmentDetector();

        [Theory]
        [InlineData("https://ara-assessment.com/assess/")]
        [InlineData("https://ara-assessment.com/assess")]
        [InlineData("https://ara-assessment.com/assess/Login")]
        [InlineData("https://ara-assessment.com/assess/Dashboard")]
        [InlineData("https://ara-assessment.com/assess/selesai")]
        [InlineData("https://ara-assessment.com/")]
        public void NormalRoutesReturnNormal(string url)
        {
            Assert.Equal(AssessmentMode.Normal, _detector.EvaluateUrl(url));
        }

        [Theory]
        [InlineData("https://ara-assessment.com/assess/Exam")]
        [InlineData("https://ara-assessment.com/ujian/mulai")]
        [InlineData("https://ara-assessment.com/exam/question/1")]
        [InlineData("https://ara-assessment.com/soal/1")]
        [InlineData("https://ara-assessment.com/essay")]
        [InlineData("https://ara-assessment.com/assessment")]
        [InlineData("https://ara-assessment.com/quiz/start")]
        public void ExamRoutesReturnExam(string url)
        {
            Assert.Equal(AssessmentMode.Exam, _detector.EvaluateUrl(url));
        }

        [Theory]
        [InlineData("strict", AssessmentMode.Exam)]
        [InlineData("essay", AssessmentMode.Exam)]
        [InlineData("exam", AssessmentMode.Exam)]
        [InlineData("STRICT", AssessmentMode.Exam)]
        [InlineData("normal", AssessmentMode.Normal)]
        [InlineData("idle", AssessmentMode.Normal)]
        [InlineData("", AssessmentMode.Normal)]
        [InlineData("unknown", AssessmentMode.Normal)]
        public void JsModesMapToAssessmentModes(string mode, AssessmentMode expected)
        {
            Assert.Equal(expected, _detector.EvaluateMode(mode));
        }

        [Fact]
        public void InjectedScriptIsEmbedded()
        {
            Assert.False(string.IsNullOrWhiteSpace(AssessmentDetector.InjectedScript));
            Assert.Contains("recordAraDesktopViolation", AssessmentDetector.InjectedScript);
        }
    }
}
