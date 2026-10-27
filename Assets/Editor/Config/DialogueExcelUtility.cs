using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security;
using System.Text;

public static class DialogueExcelUtility
{
    public static readonly string[] Headers =
    {
        "dialogueId",
        "lineIndex",
        "speakerName",
        "localizationKey",
        "portraitResource",
        "standingResource",
        "expression",
        "style",
        "autoPlayDelay",
        "option1LocalizationKey",
        "option1NextDialogueId",
        "option1Condition",
        "option1Effects",
        "option2LocalizationKey",
        "option2NextDialogueId",
        "option2Condition",
        "option2Effects",
        "option3LocalizationKey",
        "option3NextDialogueId",
        "option3Condition",
        "option3Effects",
        "option4LocalizationKey",
        "option4NextDialogueId",
        "option4Condition",
        "option4Effects"
    };

    public static void ExportXlsx(DialogueTable table, string outputPath)
    {
        if (table == null)
        {
            throw new ArgumentNullException(nameof(table));
        }

        if (string.IsNullOrEmpty(outputPath))
        {
            throw new ArgumentException("Output path is empty.", nameof(outputPath));
        }

        string directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        List<List<string>> rows = BuildRows(table);
        using (FileStream stream = File.Create(outputPath))
        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create))
        {
            WriteText(archive, "[Content_Types].xml", GetContentTypesXml());
            WriteText(archive, "_rels/.rels", GetRootRelsXml());
            WriteText(archive, "xl/workbook.xml", GetWorkbookXml());
            WriteText(archive, "xl/_rels/workbook.xml.rels", GetWorkbookRelsXml());
            WriteText(archive, "xl/styles.xml", GetStylesXml());
            WriteText(archive, "xl/worksheets/sheet1.xml", GetSheetXml(rows));
        }
    }

    private static List<List<string>> BuildRows(DialogueTable table)
    {
        List<List<string>> rows = new List<List<string>>
        {
            new List<string>(Headers)
        };

        List<DialogueTableRow> lines = new List<DialogueTableRow>();
        if (table.lines != null)
        {
            foreach (DialogueTableRow row in table.lines)
            {
                if (row != null)
                {
                    lines.Add(row);
                }
            }
        }

        lines.Sort((left, right) =>
        {
            int idCompare = string.Compare(left.dialogueId, right.dialogueId, StringComparison.Ordinal);
            return idCompare != 0 ? idCompare : left.lineIndex.CompareTo(right.lineIndex);
        });

        foreach (DialogueTableRow row in lines)
        {
            List<string> cells = new List<string>
            {
                row.dialogueId,
                row.lineIndex.ToString(CultureInfo.InvariantCulture),
                row.speakerName,
                row.localizationKey,
                row.portraitResource,
                row.standingResource,
                row.expression,
                row.style.ToString(),
                row.autoPlayDelay.ToString(CultureInfo.InvariantCulture)
            };

            for (int i = 0; i < 4; i++)
            {
                DialogueTableOptionRow option = row.options != null && i < row.options.Count ? row.options[i] : null;
                cells.Add(option != null ? option.localizationKey : string.Empty);
                cells.Add(option != null ? option.nextDialogueId : string.Empty);
                cells.Add(option != null ? option.condition : string.Empty);
                cells.Add(option != null ? option.effects : string.Empty);
            }

            rows.Add(cells);
        }

        return rows;
    }

    private static string GetSheetXml(List<List<string>> rows)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        builder.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">");
        builder.Append("<sheetData>");

        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            builder.Append("<row r=\"").Append(rowIndex + 1).Append("\">");
            List<string> row = rows[rowIndex];
            for (int columnIndex = 0; columnIndex < row.Count; columnIndex++)
            {
                string cellRef = GetCellReference(columnIndex, rowIndex + 1);
                builder.Append("<c r=\"").Append(cellRef).Append("\" t=\"inlineStr\"><is><t>");
                builder.Append(Escape(row[columnIndex]));
                builder.Append("</t></is></c>");
            }
            builder.Append("</row>");
        }

        builder.Append("</sheetData>");
        builder.Append("</worksheet>");
        return builder.ToString();
    }

    private static string GetCellReference(int columnIndex, int rowNumber)
    {
        int dividend = columnIndex + 1;
        string columnName = string.Empty;

        while (dividend > 0)
        {
            int modulo = (dividend - 1) % 26;
            columnName = Convert.ToChar('A' + modulo) + columnName;
            dividend = (dividend - modulo) / 26;
        }

        return columnName + rowNumber.ToString(CultureInfo.InvariantCulture);
    }

    private static string Escape(string value)
    {
        return SecurityElement.Escape(value ?? string.Empty);
    }

    private static void WriteText(ZipArchive archive, string path, string content)
    {
        ZipArchiveEntry entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using (StreamWriter writer = new StreamWriter(entry.Open(), new UTF8Encoding(false)))
        {
            writer.Write(content);
        }
    }

    private static string GetContentTypesXml()
    {
        return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
               "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
               "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
               "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
               "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
               "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
               "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
               "</Types>";
    }

    private static string GetRootRelsXml()
    {
        return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
               "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
               "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
               "</Relationships>";
    }

    private static string GetWorkbookXml()
    {
        return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
               "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
               "<sheets><sheet name=\"Dialogue\" sheetId=\"1\" r:id=\"rId1\"/></sheets>" +
               "</workbook>";
    }

    private static string GetWorkbookRelsXml()
    {
        return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
               "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
               "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
               "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>" +
               "</Relationships>";
    }

    private static string GetStylesXml()
    {
        return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
               "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
               "<fonts count=\"1\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts>" +
               "<fills count=\"1\"><fill><patternFill patternType=\"none\"/></fill></fills>" +
               "<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>" +
               "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
               "<cellXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/></cellXfs>" +
               "</styleSheet>";
    }
}
