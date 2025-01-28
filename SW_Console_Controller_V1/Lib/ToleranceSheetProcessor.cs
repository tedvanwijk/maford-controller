using OfficeOpenXml;
using OfficeOpenXml.Export.ToDataTable;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SW_Console_Controller_V1.Lib
{
    internal class ToleranceSheetProcessor
    {
        private Properties Properties;
        private ExcelWorksheet Worksheet;

        public ToleranceSheetProcessor(Properties properties)
        {
            ExcelPackage.LicenseContext = LicenseContext.Commercial;
            Properties = properties;
        }

        public DataTable GetToleranceData()
        {
            string path = Path.Combine(Properties.ToolSeriesPath, Properties.ToolSeriesFileName);
            FileInfo fileInfo = new FileInfo(path);
            if (!fileInfo.Exists)
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("SOFTWARE_NAME");
                dt.Columns.Add("TOL_TYPE");
                dt.Columns.Add("TABLE_TYPE");
                return dt;
            }
            ExcelPackage package = new ExcelPackage(fileInfo);
            Worksheet = package.Workbook.Worksheets["TOLERANCE_DATA"];
            EnterInputs();
            package.Workbook.Calculate();
            ExcelRange output = Worksheet.Cells[Properties.ToolSeriesOutputRange];
            DataTable table = ConvertToDataTable(output);
            // add a column to keep track of which tolerances have been implemented in the tolerances of the drawing, so the remaining data can be written to a table
            //DataColumn dataColumn = new DataColumn("ToleranceInDrawing", typeof(bool));
            //dataColumn.DefaultValue = false;
            //table.Columns.Add(dataColumn);
            return table;
        }

        private DataTable ConvertToDataTable(ExcelRange range)
        {
            int rowCount = range.Rows;
            int colCount = range.Columns;
            int rowOffset = range.Start.Row;
            int colOffset = range.Start.Column;
            DataTable table = new DataTable();
            for (int col = 0; col < colCount; col++)
            {
                table.Columns.Add(Worksheet.Cells[1, col + colOffset].Text);
            }
            DataColumn valDigits = new DataColumn("VAL_DIGITS")
            {
                DataType = typeof(int),
                DefaultValue = 0
            };
            table.Columns.Add(valDigits);

            for (int row = rowOffset; row < (rowOffset + rowCount); row++)
            {
                DataRow newRow = table.NewRow();
                // - 2 in the for loop limit to account for the 2 digit columns
                for (int col = colOffset; col < (colOffset + colCount); col++)
                {
                    //object newValue = range.GetCellValue<decimal>(row, col);
                    string cellValue = Worksheet.Cells[row, col].Text.Replace(',', '.');
                    // C# requires all data in a column to be of the same type, which is not the case in the sheet, so we just use strings and cast/convert when adding tolerances
                    //object newValue;
                    //if (cellValue.Length > 0 && decimal.TryParse(cellValue, out _))
                    //{
                    //    newValue = decimal.Parse(cellValue, CultureInfo.InvariantCulture);
                    //} else
                    //{
                    //    newValue = cellValue;
                    //}
                    newRow[col - colOffset] = cellValue;
                    if ((table.Columns[col - colOffset].ColumnName == "MIN_VAL" || table.Columns[col - colOffset].ColumnName == "MAX_VAL") && double.TryParse(cellValue, out _))
                    {
                        string[] numberParts = cellValue.Split('.');
                        int digits = numberParts.Length == 1 ? 0 : numberParts[1].Length;
                        // TODO: check with nate if this is correct. It currently chooses the largest precision digit for the tolerance, but not sure if this is the correct way
                        if (digits > (int)newRow["VAL_DIGITS"]) newRow["VAL_DIGITS"] = digits;
                    }
                }
                table.Rows.Add(newRow);
            }

            return table;
        }

        private void EnterInputs()
        {
            if (Properties.ToolSeriesInputs.Length == 0) return;
            string inputRange = Properties.ToolSeriesInputRange;
            ExcelRange input = Worksheet.Cells[inputRange];
            int columnCount = input.Columns;
            int rowCount = input.Rows;

            for (int col = 0; col < columnCount; col++)
            {
                for (int row = 0; row < rowCount; row++)
                {
                    string val = Properties.ToolSeriesInputs[row];
                    float valNumeric;
                    bool isNumber = float.TryParse(val, out valNumeric);
                    if (isNumber)
                    {
                        input.SetCellValue(
                            row,
                            col,
                            valNumeric
                            );
                    }
                    else
                    {
                        input.SetCellValue(
                            row,
                            col,
                            val
                            );
                    }

                }
            }
        }
    }
}
