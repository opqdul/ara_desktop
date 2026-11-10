using System;
using Ara.Core;
using Xunit;

namespace Ara.Tests
{
    public class OfficeDocumentServiceTests
    {
        [Theory]
        [InlineData("Jawaban.DOCX", OfficeDocumentKind.Word)]
        [InlineData("nilai.xls", OfficeDocumentKind.Excel)]
        [InlineData("presentasi.pptx", OfficeDocumentKind.PowerPoint)]
        public void DetectKindFromFileName(string fileName, OfficeDocumentKind expected)
        {
            Assert.Equal(expected, OfficeDocumentService.DetectKind(fileName, null, null));
        }

        [Fact]
        public void DetectKindFromUrlIgnoresQueryString()
        {
            var url = new Uri("https://ara-assessment.com/files/template.xlsx?token=abc#x");
            Assert.Equal(OfficeDocumentKind.Excel, OfficeDocumentService.DetectKind(null, url, null));
        }

        [Fact]
        public void DetectKindFromMimeType()
        {
            const string mime = "application/vnd.openxmlformats-officedocument.presentationml.presentation; charset=binary";
            Assert.Equal(OfficeDocumentKind.PowerPoint, OfficeDocumentService.DetectKind(null, null, mime));
        }

        [Fact]
        public void DetectKindReturnsNullForNonOffice()
        {
            Assert.Null(OfficeDocumentService.DetectKind("soal.pdf", null, "application/pdf"));
        }

        [Fact]
        public void IsOfficeDocumentUrlOnlyMatchesHttpOfficeFiles()
        {
            Assert.True(OfficeDocumentService.IsOfficeDocumentUrl(new Uri("https://a.com/x/tugas.docx")));
            Assert.True(OfficeDocumentService.IsOfficeDocumentUrl(new Uri("http://a.com/x/data.XLSX?d=1")));
            Assert.False(OfficeDocumentService.IsOfficeDocumentUrl(new Uri("https://a.com/ujian/soal")));
            Assert.False(OfficeDocumentService.IsOfficeDocumentUrl(new Uri("blob:https://a.com/1234")));
            Assert.False(OfficeDocumentService.IsOfficeDocumentUrl(new Uri("ms-word:ofe|u|https://a.com/a.docx")));
        }

        [Fact]
        public void SanitizeFileNameRemovesIllegalCharactersAndTraversal()
        {
            Assert.Equal("_.._etc_passwd", OfficeDocumentService.SanitizeFileName("../../etc/passwd"));
            Assert.Equal("a_b_c__d_e_f_g.docx", OfficeDocumentService.SanitizeFileName("a:b*c?\"d<e>f|g.docx"));
            Assert.Equal("dokumen", OfficeDocumentService.SanitizeFileName("   "));
        }

        [Fact]
        public void SanitizeFileNameLimitsLengthButKeepsExtension()
        {
            var result = OfficeDocumentService.SanitizeFileName(new string('a', 300) + ".pptx");
            Assert.Equal(120, result.Length);
            Assert.EndsWith(".pptx", result);
        }

        [Fact]
        public void FileNameFromContentDispositionSupportsRfc5987()
        {
            var header = "attachment; filename=\"fallback.docx\"; filename*=UTF-8''Tugas%20Akhir%202026.docx";
            Assert.Equal("Tugas Akhir 2026.docx", OfficeDocumentService.FileNameFromContentDisposition(header));
        }

        [Fact]
        public void ResolveFileNameUsesSuggestedNameThenDispositionThenUrlThenMime()
        {
            Assert.Equal(
                "jawaban.docx",
                OfficeDocumentService.ResolveFileName(
                    "jawaban.docx",
                    "attachment; filename=\"other.docx\"",
                    new Uri("https://a.com/download/123"),
                    null));

            Assert.Equal(
                "slide.pptx",
                OfficeDocumentService.ResolveFileName(
                    null,
                    "attachment; filename=\"slide.pptx\"",
                    new Uri("https://a.com/download/123"),
                    null));

            Assert.Equal(
                "Data Nilai.xlsx",
                OfficeDocumentService.ResolveFileName(
                    null,
                    null,
                    new Uri("https://a.com/files/Data%20Nilai.xlsx"),
                    null));

            Assert.Equal(
                "123.docx",
                OfficeDocumentService.ResolveFileName(
                    null,
                    null,
                    new Uri("https://a.com/download/123"),
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document"));
        }

        [Fact]
        public void MimeTypeForMapsKnownExtensions()
        {
            Assert.Equal(
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                OfficeDocumentService.MimeTypeFor("a.xlsx", null));
            Assert.Equal("application/pdf", OfficeDocumentService.MimeTypeFor("a.pdf", "application/pdf"));
        }
    }
}
