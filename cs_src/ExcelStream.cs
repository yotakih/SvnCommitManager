using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Excel = Microsoft.Office.Interop.Excel;

namespace SvnCommitManager
{
  class ExcelStream
  {
    private Excel.Application exlapp = null;
    private Excel.Workbook wrkbook = null;
    private long curRowCnt = 1;
    private Excel.Worksheet curSht = null;

    public ExcelStream()
    {
      exlapp = new Excel.Application();
      exlapp.Visible = false;
    }

    public void opnWrkBook(string flnm)
    {
      wrkbook = exlapp.Workbooks.Add();
      wrkbook.SaveAs(Filename: flnm);
    }

    public Excel.Worksheet addShtAtLst(string shtnm)
    {
      wrkbook.Worksheets.Add(After: wrkbook.Worksheets[wrkbook.Worksheets.Count]);
      Excel.Worksheet sht = (Excel.Worksheet)wrkbook.Worksheets[wrkbook.Worksheets.Count];
      sht.Name = shtnm;
      sht.Move(Before: wrkbook.Worksheets["Sheet1"]);
      return sht;
    }

    public void addRevSht(string shtnm,string logMes)
    {
      var sht = addShtAtLst(shtnm);
      sht.Cells[1, 1] = @"Log Message:";
      sht.Range[sht.Cells[2, 2], sht.Cells[5, 10]].Merge();
      sht.Cells[2,2] = logMes;

      var chgPthRow = 5 + 1;
      sht.Cells[chgPthRow, 1] = @"ChangedPaths:";
      sht.Cells[chgPthRow + 1, 2] = @"action";
      sht.Cells[chgPthRow + 1, 3] = @"folder";
      sht.Cells[chgPthRow + 1, 4] = @"file";
      sht.Cells[chgPthRow + 1, 5] = @"抽出対象";
      this.curSht = sht;
      this.curRowCnt = chgPthRow + 1;
    }

    public void wrtRevRow(string[] cols)
    {
      this.curRowCnt++;
      for (var idx = 0; idx < cols.Count(); idx++)
        this.curSht.Cells[this.curRowCnt, idx+1+1] = cols[idx];
    }

    public void save()
    {
      if(this.wrkbook != null)
        this.wrkbook.Save();
    }
    
    public void close()
    {
      if(this.wrkbook != null)
        this.wrkbook.Close();
    }
  }
}
