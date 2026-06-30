using ClosedXML.Excel;

namespace AppGen.Engine;

internal static class SpecWorkbookValidation
{
    public static void ApplyList(IXLRange cells, string listFormula, bool allowBlank = true)
    {
        var validation = cells.CreateDataValidation();
        validation.List(listFormula);
        validation.InCellDropdown = true;
        validation.IgnoreBlanks = allowBlank;
    }

    public static void ApplyListFromListsSheet(IXLWorkbook workbook, IXLCell cell, int listColumn) =>
        ApplyListFromListsSheet(workbook, cell.AsRange(), listColumn);

    public static void ApplyListFromListsSheet(IXLWorkbook workbook, IXLRange cells, int listColumn)
    {
        var lists = workbook.Worksheet(SpecWorkbookLists.ListsSheetName);
        var lastRow = lists.Column(listColumn).LastCellUsed()?.Address.RowNumber ?? 1;
        if (lastRow < 2)
            return;

        var columnLetter = XLHelper.GetColumnLetterFromNumber(listColumn);
        ApplyList(cells, $"='{SpecWorkbookLists.ListsSheetName}'!${columnLetter}$2:${columnLetter}${lastRow}");
    }

    public static void ApplyEntityNameList(IXLWorkbook workbook, IXLRange cells) =>
        ApplyList(cells, $"={SpecWorkbookSheets.Entities}!$A$2:$A${SpecWorkbookLists.DataEntryMaxRow}", allowBlank: true);

    public static void FreezeHeaderRow(IXLWorksheet sheet) =>
        sheet.SheetView.FreezeRows(1);

    public static void StyleExampleRow(IXLWorksheet sheet, int row, int lastColumn)
    {
        if (lastColumn < 1)
            return;

        var range = sheet.Range(row, 1, row, lastColumn);
        range.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF8E7");
        range.Style.Font.Italic = true;
    }

    public static void LockFieldNameColumn(IXLWorksheet sheet, int lastRow)
    {
        if (lastRow < 2)
            return;

        var fieldColumn = sheet.Range(2, 1, lastRow, 1);
        fieldColumn.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8E8E8");
        fieldColumn.Style.Font.Bold = true;
        fieldColumn.Style.Protection.SetLocked(true);

        var valueColumn = sheet.Range(2, 2, lastRow, 2);
        valueColumn.Style.Protection.SetLocked(false);

        sheet.Protect()
            .AllowElement(XLSheetProtectionElements.SelectLockedCells)
            .AllowElement(XLSheetProtectionElements.SelectUnlockedCells);
    }
}
