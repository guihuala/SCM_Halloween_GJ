using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml.Linq;

public class ExcelSheet
{
    public string Name { get; set; }
    public List<List<string>> Rows { get; } = new List<List<string>>();
}

public class ExcelSheetInfo
{
    public string Name { get; set; }
    public string Path { get; set; }
}

public static class ExcelTableReader
{
    public static List<ExcelSheetInfo> GetSheets(string xlsxPath)
    {
        if (string.IsNullOrEmpty(xlsxPath) || !File.Exists(xlsxPath))
        {
            throw new FileNotFoundException("Excel file not found.", xlsxPath);
        }

        using (FileStream stream = File.OpenRead(xlsxPath))
        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
        {
            return ReadSheetInfos(archive);
        }
    }

    public static ExcelSheet ReadFirstSheet(string xlsxPath)
    {
        List<ExcelSheetInfo> sheets = GetSheets(xlsxPath);
        if (sheets.Count == 0)
        {
            throw new InvalidDataException("Only .xlsx files with at least one worksheet are supported.");
        }

        return ReadSheet(xlsxPath, sheets[0].Name);
    }

    public static ExcelSheet ReadSheet(string xlsxPath, string sheetName)
    {
        if (string.IsNullOrEmpty(xlsxPath) || !File.Exists(xlsxPath))
        {
            throw new FileNotFoundException("Excel file not found.", xlsxPath);
        }

        using (FileStream stream = File.OpenRead(xlsxPath))
        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
        {
            List<string> sharedStrings = ReadSharedStrings(archive);
            ExcelSheetInfo sheetInfo = FindSheet(archive, sheetName);
            ZipArchiveEntry sheetEntry = archive.GetEntry(sheetInfo.Path);

            if (sheetEntry == null)
            {
                throw new InvalidDataException($"Worksheet xml not found: {sheetInfo.Path}");
            }

            ExcelSheet sheet = ReadSheetXml(sheetEntry, sharedStrings);
            sheet.Name = sheetInfo.Name;
            return sheet;
        }
    }

    private static ExcelSheetInfo FindSheet(ZipArchive archive, string sheetName)
    {
        List<ExcelSheetInfo> sheets = ReadSheetInfos(archive);
        if (sheets.Count == 0)
        {
            throw new InvalidDataException("Only .xlsx files with at least one worksheet are supported.");
        }

        if (!string.IsNullOrEmpty(sheetName))
        {
            foreach (ExcelSheetInfo sheet in sheets)
            {
                if (sheet.Name == sheetName)
                {
                    return sheet;
                }
            }
        }

        return sheets[0];
    }

    private static List<ExcelSheetInfo> ReadSheetInfos(ZipArchive archive)
    {
        ZipArchiveEntry workbookEntry = archive.GetEntry("xl/workbook.xml");
        ZipArchiveEntry relationshipsEntry = archive.GetEntry("xl/_rels/workbook.xml.rels");

        if (workbookEntry == null || relationshipsEntry == null)
        {
            throw new InvalidDataException("Invalid .xlsx file. Workbook metadata is missing.");
        }

        XNamespace workbookNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace relationNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        XNamespace packageRelationNs = "http://schemas.openxmlformats.org/package/2006/relationships";

        Dictionary<string, string> relationTargets = new Dictionary<string, string>();
        using (Stream stream = relationshipsEntry.Open())
        {
            XDocument relationships = XDocument.Load(stream);
            foreach (XElement relationship in relationships.Root.Elements(packageRelationNs + "Relationship"))
            {
                string id = (string)relationship.Attribute("Id");
                string target = (string)relationship.Attribute("Target");
                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(target))
                {
                    relationTargets[id] = NormalizeSheetPath(target);
                }
            }
        }

        List<ExcelSheetInfo> sheets = new List<ExcelSheetInfo>();
        using (Stream stream = workbookEntry.Open())
        {
            XDocument workbook = XDocument.Load(stream);
            foreach (XElement sheet in workbook.Descendants(workbookNs + "sheet"))
            {
                string name = (string)sheet.Attribute("name");
                string relationId = (string)sheet.Attribute(relationNs + "id");

                if (!string.IsNullOrEmpty(name) &&
                    !string.IsNullOrEmpty(relationId) &&
                    relationTargets.TryGetValue(relationId, out string path))
                {
                    sheets.Add(new ExcelSheetInfo
                    {
                        Name = name,
                        Path = path
                    });
                }
            }
        }

        return sheets;
    }

    private static string NormalizeSheetPath(string target)
    {
        target = target.Replace("\\", "/");
        return target.StartsWith("xl/", StringComparison.Ordinal) ? target : "xl/" + target;
    }

    private static List<string> ReadSharedStrings(ZipArchive archive)
    {
        List<string> sharedStrings = new List<string>();
        ZipArchiveEntry entry = archive.GetEntry("xl/sharedStrings.xml");

        if (entry == null)
        {
            return sharedStrings;
        }

        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        using (Stream stream = entry.Open())
        {
            XDocument document = XDocument.Load(stream);
            foreach (XElement item in document.Root.Elements(ns + "si"))
            {
                string value = string.Empty;

                foreach (XElement text in item.Descendants(ns + "t"))
                {
                    value += text.Value;
                }

                sharedStrings.Add(value);
            }
        }

        return sharedStrings;
    }

    private static ExcelSheet ReadSheetXml(ZipArchiveEntry sheetEntry, List<string> sharedStrings)
    {
        ExcelSheet sheet = new ExcelSheet();
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        using (Stream stream = sheetEntry.Open())
        {
            XDocument document = XDocument.Load(stream);

            foreach (XElement row in document.Descendants(ns + "row"))
            {
                List<string> cells = new List<string>();

                foreach (XElement cell in row.Elements(ns + "c"))
                {
                    string cellReference = (string)cell.Attribute("r");
                    int columnIndex = GetColumnIndex(cellReference);

                    while (cells.Count <= columnIndex)
                    {
                        cells.Add(string.Empty);
                    }

                    cells[columnIndex] = ReadCellValue(cell, sharedStrings, ns);
                }

                sheet.Rows.Add(cells);
            }
        }

        return sheet;
    }

    private static string ReadCellValue(XElement cell, List<string> sharedStrings, XNamespace ns)
    {
        string cellType = (string)cell.Attribute("t");

        if (cellType == "inlineStr")
        {
            XElement inlineText = cell.Element(ns + "is")?.Element(ns + "t");
            return inlineText != null ? inlineText.Value : string.Empty;
        }

        XElement valueElement = cell.Element(ns + "v");
        string rawValue = valueElement != null ? valueElement.Value : string.Empty;

        if (cellType == "s" && int.TryParse(rawValue, out int sharedStringIndex))
        {
            if (sharedStringIndex >= 0 && sharedStringIndex < sharedStrings.Count)
            {
                return sharedStrings[sharedStringIndex];
            }
        }

        return rawValue;
    }

    private static int GetColumnIndex(string cellReference)
    {
        if (string.IsNullOrEmpty(cellReference))
        {
            return 0;
        }

        int columnIndex = 0;

        foreach (char character in cellReference)
        {
            if (!char.IsLetter(character))
            {
                break;
            }

            columnIndex *= 26;
            columnIndex += char.ToUpperInvariant(character) - 'A' + 1;
        }

        return Math.Max(0, columnIndex - 1);
    }
}
