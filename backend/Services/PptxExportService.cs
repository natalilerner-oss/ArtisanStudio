using ArtisanStudio.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;

using D = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace ArtisanStudio.Services;

public static class PptxExportService
{
    public static byte[] GeneratePptx(Presentation presentation)
    {
        using var stream = new MemoryStream();
        using (var pptDoc = PresentationDocument.Create(stream, PresentationDocumentType.Presentation, true))
        {
            var presentationPart = pptDoc.AddPresentationPart();
            presentationPart.Presentation = new P.Presentation();

            var slideMasterPart = presentationPart.AddNewPart<SlideMasterPart>("rId1");
            var slideLayoutPart = slideMasterPart.AddNewPart<SlideLayoutPart>("rId1");

            // Create minimal slide master
            slideMasterPart.SlideMaster = CreateSlideMaster();
            slideLayoutPart.SlideLayout = CreateSlideLayout();

            // Create slides
            var slideIdList = new SlideIdList();
            uint slideId = 256;

            foreach (var slide in presentation.Slides)
            {
                var slidePart = presentationPart.AddNewPart<SlidePart>($"rId{slideId}");
                slidePart.Slide = CreateSlide(slide, presentation.Metadata.Style);

                // Link slide to layout
                slidePart.AddPart(slideLayoutPart, "rId1");

                // Add speaker notes if present
                if (!string.IsNullOrEmpty(slide.SpeakerNotes))
                {
                    AddSpeakerNotes(slidePart, slide.SpeakerNotes);
                }

                slideIdList.Append(new SlideId { Id = slideId, RelationshipId = $"rId{slideId}" });
                slideId++;
            }

            presentationPart.Presentation.SlideIdList = slideIdList;

            // Add required slide master/layout references
            var slideMasterIdList = new SlideMasterIdList();
            slideMasterIdList.Append(new SlideMasterId { Id = 2147483648, RelationshipId = "rId1" });
            presentationPart.Presentation.SlideMasterIdList = slideMasterIdList;

            // Set slide size (16:9 or 4:3)
            var slideSize = presentation.Metadata.AspectRatio == "4:3"
                ? new SlideSize { Cx = 9144000, Cy = 6858000, Type = SlideSizeValues.Screen4x3 }
                : new SlideSize { Cx = 12192000, Cy = 6858000, Type = SlideSizeValues.Screen16x9 };
            presentationPart.Presentation.SlideSize = slideSize;

            var notesSize = new NotesSize { Cx = 6858000, Cy = 9144000 };
            presentationPart.Presentation.NotesSize = notesSize;

            presentationPart.Presentation.Save();
        }

        return stream.ToArray();
    }

    private static P.Slide CreateSlide(Slide slideData, string style)
    {
        var slide = new P.Slide(
            new CommonSlideData(
                new ShapeTree(
                    new P.NonVisualGroupShapeProperties(
                        new P.NonVisualDrawingProperties { Id = 1, Name = "" },
                        new P.NonVisualGroupShapeDrawingProperties(),
                        new ApplicationNonVisualDrawingProperties()),
                    new GroupShapeProperties(new D.TransformGroup())
                )
            ),
            new ColorMapOverride(new D.MasterColorMapping())
        );

        var shapeTree = slide.CommonSlideData!.ShapeTree!;
        uint shapeId = 2;
        long yOffset = 365125; // Starting Y position

        // Add title
        if (!string.IsNullOrEmpty(slideData.Title))
        {
            var isTitleSlide = slideData.Type == "title" || slideData.Type == "closing";
            var titleY = isTitleSlide ? 2000000L : 365125L;
            var fontSize = isTitleSlide ? 4400 : 3200;

            shapeTree.Append(CreateTextShape(
                shapeId++, "Title",
                457200, titleY, 8229600, 1325563,
                slideData.Title, fontSize, true, style));

            yOffset = titleY + 1500000;
        }

        // Add subtitle
        if (!string.IsNullOrEmpty(slideData.Subtitle))
        {
            shapeTree.Append(CreateTextShape(
                shapeId++, "Subtitle",
                457200, yOffset, 8229600, 600000,
                slideData.Subtitle, 2000, false, style));
            yOffset += 750000;
        }

        // Add bullets
        if (slideData.Bullets?.Count > 0)
        {
            var bulletText = string.Join("\n", slideData.Bullets.Select(b => $"\u2022 {b}"));
            shapeTree.Append(CreateTextShape(
                shapeId++, "Content",
                457200, yOffset, 8229600, 3600000,
                bulletText, 1800, false, style));
            yOffset += 3600000;
        }

        // Add body text
        if (!string.IsNullOrEmpty(slideData.BodyText))
        {
            shapeTree.Append(CreateTextShape(
                shapeId++, "Body",
                457200, yOffset, 8229600, 2000000,
                slideData.BodyText, 1600, false, style));
        }

        // Add chart description text (since OpenXML chart embedding is complex)
        if (slideData.Chart != null)
        {
            var chartDesc = $"[{slideData.Chart.Type.ToUpper()} CHART: {string.Join(", ", slideData.Chart.Data.Labels.Zip(slideData.Chart.Data.Values, (l, v) => $"{l}: {v}"))}]";
            shapeTree.Append(CreateTextShape(
                shapeId++, "Chart",
                457200, yOffset, 8229600, 1200000,
                chartDesc, 1400, false, style));
        }

        // Add diagram description
        if (slideData.Diagram != null)
        {
            shapeTree.Append(CreateTextShape(
                shapeId++, "Diagram",
                457200, yOffset, 8229600, 1200000,
                $"[{slideData.Diagram.Type.ToUpper()} DIAGRAM]\n{slideData.Diagram.MermaidCode}", 1200, false, style));
        }

        return slide;
    }

    private static P.Shape CreateTextShape(
        uint id, string name,
        long x, long y, long cx, long cy,
        string text, int fontSize, bool bold, string style)
    {
        var textColor = style == "dark" ? "FFFFFF" : "333333";

        var shape = new P.Shape(
            new P.NonVisualShapeProperties(
                new P.NonVisualDrawingProperties { Id = id, Name = name },
                new P.NonVisualShapeDrawingProperties(
                    new D.ShapeLocks { NoGrouping = true }),
                new ApplicationNonVisualDrawingProperties()),
            new P.ShapeProperties(
                new D.Transform2D(
                    new D.Offset { X = x, Y = y },
                    new D.Extents { Cx = cx, Cy = cy })),
            new P.TextBody(
                new D.BodyProperties(),
                new D.ListStyle(),
                CreateParagraph(text, fontSize, bold, textColor)
            )
        );

        return shape;
    }

    private static D.Paragraph CreateParagraph(string text, int fontSize, bool bold, string color)
    {
        var runProperties = new D.RunProperties(
            new D.SolidFill(new D.RgbColorModelHex { Val = color }),
            new D.LatinFont { Typeface = "Calibri" },
            new D.EastAsianFont { Typeface = "Calibri" },
            new D.ComplexScriptFont { Typeface = "Calibri" }
        )
        {
            Language = "en-US",
            FontSize = fontSize,
            Bold = bold
        };

        var run = new D.Run(runProperties, new D.Text(text));

        return new D.Paragraph(
            new D.ParagraphProperties(new D.DefaultRunProperties { FontSize = fontSize }),
            run);
    }

    private static void AddSpeakerNotes(SlidePart slidePart, string notes)
    {
        var notesSlidePart = slidePart.AddNewPart<NotesSlidePart>();

        notesSlidePart.NotesSlide = new NotesSlide(
            new CommonSlideData(
                new ShapeTree(
                    new P.NonVisualGroupShapeProperties(
                        new P.NonVisualDrawingProperties { Id = 1, Name = "" },
                        new P.NonVisualGroupShapeDrawingProperties(),
                        new ApplicationNonVisualDrawingProperties()),
                    new GroupShapeProperties(new D.TransformGroup()),
                    new P.Shape(
                        new P.NonVisualShapeProperties(
                            new P.NonVisualDrawingProperties { Id = 2, Name = "Notes" },
                            new P.NonVisualShapeDrawingProperties(
                                new D.ShapeLocks { NoGrouping = true }),
                            new ApplicationNonVisualDrawingProperties(
                                new PlaceholderShape { Type = PlaceholderValues.Body, Index = 1 })),
                        new P.ShapeProperties(),
                        new P.TextBody(
                            new D.BodyProperties(),
                            new D.ListStyle(),
                            new D.Paragraph(
                                new D.Run(
                                    new D.RunProperties { Language = "en-US", FontSize = 1200 },
                                    new D.Text(notes)))))
                )
            ),
            new ColorMapOverride(new D.MasterColorMapping())
        );
    }

    private static SlideMaster CreateSlideMaster()
    {
        return new SlideMaster(
            new CommonSlideData(
                new ShapeTree(
                    new P.NonVisualGroupShapeProperties(
                        new P.NonVisualDrawingProperties { Id = 1, Name = "" },
                        new P.NonVisualGroupShapeDrawingProperties(),
                        new ApplicationNonVisualDrawingProperties()),
                    new GroupShapeProperties(new D.TransformGroup())
                )
            ),
            new ColorMapOverride(new D.MasterColorMapping()),
            new SlideLayoutIdList(
                new SlideLayoutId { Id = 2147483649, RelationshipId = "rId1" })
        );
    }

    private static SlideLayout CreateSlideLayout()
    {
        return new SlideLayout(
            new CommonSlideData(
                new ShapeTree(
                    new P.NonVisualGroupShapeProperties(
                        new P.NonVisualDrawingProperties { Id = 1, Name = "" },
                        new P.NonVisualGroupShapeDrawingProperties(),
                        new ApplicationNonVisualDrawingProperties()),
                    new GroupShapeProperties(new D.TransformGroup())
                )
            ),
            new ColorMapOverride(new D.MasterColorMapping())
        )
        { Type = SlideLayoutValues.Blank };
    }
}
