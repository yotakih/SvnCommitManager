using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SharpSvn;
using System.Text;
using System.Net;
using System.IO;

namespace SvnCommitManager
{
  class SvnConnector
  {
    private SvnClient _svncl;
    private string _src;
    private Uri _uri;
    private Uri _reporoot;
    private string _repoIdntfr;
    private string[] _filterList;

    public int viewLimit = 100;

    public void clearAuth()
    {
      //_svncl.Authentication.Clear();
      _svncl.Authentication.ClearAuthenticationCache();
    }

    public SvnConnector(Uri uri, string user, string password)
    {
      this._uri = uri;
      try
      {
        _svncl = new SvnClient();
        _svncl.Authentication.DefaultCredentials = new NetworkCredential(user, password);
      }
      catch (Exception e)
      {
        Console.WriteLine(e.Message);
      }
      this.getInfo();
    }

    public SvnConnector(string src, string user, string password)
    {
      this._src = src;
      try
      {
        _svncl = new SvnClient();
        _svncl.Authentication.DefaultCredentials = new NetworkCredential(user, password);
      }
      catch (Exception e)
      {
        Console.WriteLine(e.Message);
      }
      this.getInfo();
    }

    public Collection<SvnLogEventArgs> getLog(long strtRev, long endRev)
    {
      var args = new SvnLogArgs()
      {
        Range = new SvnRevisionRange(strtRev, endRev)
      };
      Collection<SvnLogEventArgs> result;
      this._svncl.GetLog(this._uri, args, out result);
      return result;
    }

    public Collection<SvnLogEventArgs> getHeadLog()
    {
      return this.getLog(this.viewLimit, SvnRevision.Head);
    }

    public Collection<SvnLogEventArgs> next100getLog()
    {
      this.viewLimit += 100;
      return this.getHeadLog();
    }

    private Collection<SvnLogEventArgs> getLog(int limit, SvnRevision rev)
    {
      var arg = new SvnLogArgs()
      {
        Limit = limit
        ,
        Start = rev
      };
      Collection<SvnLogEventArgs> results;
      if (!_svncl.GetLog(_uri, arg, out results))
        return null;
      return results;
    }

    public void getInfo()
    {
      //var target = new SvnUriTarget(this._uri);
      var target = new SvnPathTarget(this._src);
      SvnInfoEventArgs result;

      this._svncl.GetInfo(target, out result);

      this._uri = result.Uri;
      this._reporoot = result.RepositoryRoot;
      this._repoIdntfr =
        result.Uri.ToString().Replace(
            this._reporoot.ToString(), @""
          );

    }

    public void getChangeItem(SvnLogEventArgs log)
    {

      var tmp = new Collection<SvnChangeItem>();
      foreach (var i in log.ChangedPaths)
        if (i.NodeKind == SvnNodeKind.File)
          tmp.Add(i);
    }

    public void createAutoCommitBat(Collection<SvnLogEventArgs> logs, string toRootPath, string tmpKickBatPath, bool excDel)
    {
      if (!File.Exists(tmpKickBatPath))
        throw new FileNotFoundException(
            string.Format(@"テンプレートバッチファイルがありません。:{0}", tmpKickBatPath)
        );

      var trgtLst = new Collection<long[]>();
      var expDir = Path.Combine(toRootPath, @"10_抽出ファイル");
      var chkDir = Path.Combine(toRootPath, @"20_対象一覧CSV");
      var existTrgtCmd = @"rem rev:{0}" + Environment.NewLine +
                         @"call ""%workDir%\{0}.bat"" ""{1}""" + Environment.NewLine;
      var notExistTrgtCmd = @"rem rev:{0} 対象なし" + Environment.NewLine +
                            @"rem call %workDir%\{0}.bat ""{1}""" + Environment.NewLine;
      if (!Directory.Exists(toRootPath))
        Directory.CreateDirectory(toRootPath);
      if (!Directory.Exists(expDir))
        Directory.CreateDirectory(expDir);
      if (!Directory.Exists(chkDir))
        Directory.CreateDirectory(chkDir);

      foreach (var log in logs)
      {
        if (exportFileFromLog(log, expDir, chkDir, excDel))
        {
          trgtLst.Add(new long[] { 1, log.Revision });
        }
        else
        {
          trgtLst.Add(new long[] { -1, log.Revision });
        }
      }
      trgtLst = new Collection<long[]>(trgtLst.OrderBy(t => t[1]).Select(t => t).ToArray());
      var batFlNm = string.Format(@"r{0}to{1}.bat", trgtLst[0][1], trgtLst[trgtLst.Count - 1][1]);
      using (var sw = createAutoBatStrm(expDir, batFlNm))
      {
        foreach (var trgt in trgtLst)
        {
          if (trgt[0] > 0)
          {
            sw.WriteLine(string.Format(existTrgtCmd, retPadLeftZero(trgt[1]), @"%dstDir%"));
          }
          else
          {
            sw.WriteLine(string.Format(notExistTrgtCmd, retPadLeftZero(trgt[1]), @"dstDir%"));
          }
        }
      }

      var kickBatPath = Path.Combine(toRootPath, @"autoCommit.bat");
      File.Copy(tmpKickBatPath, kickBatPath);
      string cmdtxt;
      using (var sr = new StreamReader(kickBatPath, Encoding.GetEncoding(932)))
        cmdtxt = sr.ReadToEnd();
      using (var sw = new StreamWriter(kickBatPath, false, Encoding.GetEncoding(932)))
        sw.WriteLine(string.Format(cmdtxt, batFlNm));
    }

    private StreamWriter createAutoBatStrm(string workDir, string batFlNm)
    {
      var path = Path.Combine(workDir, batFlNm);
      var sw = new StreamWriter(path, false, Encoding.GetEncoding(932));
      sw.WriteLine(
        @"@echo off" + Environment.NewLine +
        @"" + Environment.NewLine +
        @"if ""%~1""=="""" ( " + Environment.NewLine +
        @"  echo 引数に据え付け先の作業フォルダを指定してください" + Environment.NewLine +
        @"  exit /b" + Environment.NewLine +
        @")" + Environment.NewLine +
        @"if not exist ""%~1"" ( " + Environment.NewLine +
        @"  echo 据え付け先の作業コピーが見つかりません" + Environment.NewLine +
        @"  exit /b" + Environment.NewLine +
        @")" + Environment.NewLine +
        @"set dstDir=%~1" + Environment.NewLine +
        @"set workDir=%~dp0" + Environment.NewLine +
        @"" + Environment.NewLine + 
        @"rem 認証情報登録と作業フォルダ確認のためsvn ls を実行する" + Environment.NewLine + 
        @"svn ls ""%dstDir%"">NUL" + Environment.NewLine +
        @"if not %errorlevel%==0 (" + Environment.NewLine + 
        @"    echo ログインできなかったか、作業フォルダパスが不正です。" + Environment.NewLine +
        @"    echo 作業フォルダパス：%dstDir%" + Environment.NewLine + 
        @"    exit /b" + Environment.NewLine + 
        @")" + Environment.NewLine + 
        @"" + Environment.NewLine
      );
      return sw;
    }

    public bool exportFileFromLog(SvnLogEventArgs log, string expdir, string csvdir, bool excDel)
    {
      var revname = retPadLeftZero(log.Revision);
      var toRevPath = expdir + @"\" + @"r" + revname;
      var trgtFlLst = this.filterLogFiles(log.ChangedPaths, this._filterList, csvdir, revname, excDel);
      if (trgtFlLst.Count == 0)
      {
        Directory.CreateDirectory(toRevPath + @"_対象なし");
        return false;
      }

      var root = this._reporoot;
      var args = new SvnExportArgs()
      {
        Depth = SvnDepth.Files
      };
      var addCmd = @"echo F | xcopy /v /y ""{0}"" ""{1}"" " + Environment.NewLine +
                   @"svn add              ""{1}"" ";
      var delCmd = @"if exist             ""{0}"" ( " + Environment.NewLine +
                   @"  svn delete         ""{0}"" " + Environment.NewLine +
                   @")";
      Directory.CreateDirectory(toRevPath);
      var sw = createCopyBatStrm(expdir, revname);
      foreach (var i in trgtFlLst)
      {
        var flPth = getFilePath(i.Path);
        var topath = toRevPath + flPth;
        if (i.Action != SvnChangeAction.Delete)
        {
          var srcp = Path.Combine(root.ToString()
            , i.RepositoryPath.ToString());
          var todir = Path.GetDirectoryName(topath);
          if (!Directory.Exists(todir))
            Directory.CreateDirectory(todir);
          _svncl.Export(new SvnUriTarget(srcp, log.Revision), topath, args);
          sw.WriteLine(string.Format(addCmd, topath, @"%dstRoot%" + flPth));
        }
        else
        {
          sw.WriteLine(string.Format(delCmd, @"%dstRoot" + flPth));
        }
      }
      this.setCommitComment(expdir, sw, log, revname);
      sw.Close();

      return true;
    }

    private string getFilePath(string p)
    {
      return p.Replace(this._repoIdntfr, "").Replace(@"/", @"\");
    }

    private Collection<SvnChangeItem> filterLogFiles(Collection<SvnChangeItem> items
                                                    , string[] fltrs, string dir, string revname, bool excDel)
    {
      var fltrRsltPath = Path.Combine(dir,@"r" + revname + @".csv");
      var lst = new Collection<SvnChangeItem>();
      var dlms = ',';
      using (var sw = new StreamWriter(fltrRsltPath, false, Encoding.GetEncoding(932)))
      {
        //タイトル行を書き込み
        sw.WriteLine(string.Format(@"action{0}folder{0}file{0}抽出対象", dlms));
        foreach (var i in items)
        {
          if (i.NodeKind != SvnNodeKind.File)
            continue;
          var path = i.Path.Replace(@"/", @"\");
          sw.Write(@"{1}{0}{2}{0}{3}{0}", dlms, i.Action.ToString()
                                        , Path.GetDirectoryName(path), Path.GetFileName(path));
          var hitFlg = false;
          if (i.Action != SvnChangeAction.Delete || excDel)
          {
            var tmp = i.Path.ToLower();
            foreach (var f in fltrs)
            {
              if (tmp.IndexOf(f) > -1)
              {
                hitFlg = true;
                break;
              }
            }
          }
          if (hitFlg)
          {
            lst.Add(i);
            sw.WriteLine(@"○");
          }
          else
          {
            sw.WriteLine(@"");
          }
        }
      }
      return lst;
    }

    private StreamWriter createCopyBatStrm(string dir, string revname)
    {
      var path = dir + @"\" + @"r" + revname + @".bat";
      var st = new StreamWriter(path, false, Encoding.GetEncoding(932));
      //ヘッダ部分を作成
      st.WriteLine(
        @"@echo off" + Environment.NewLine +
        @"" + Environment.NewLine +
        @"if ""%~1"" == """" ( " + Environment.NewLine +
        @"  echo 引数に据付先の作業コピーを指定してください" + Environment.NewLine +
        @"  exit /b" + Environment.NewLine +
        @")" + Environment.NewLine +
        @"if not exist ""%~1"" ( " + Environment.NewLine +
        @"  echo 据付先が見つかりません" + Environment.NewLine +
        @"  exit /b" + Environment.NewLine +
        @")" + Environment.NewLine +
        @"set dstRoot=%~1" + Environment.NewLine
      );
      return st;
    }

    public void readFilterFile(string filePath)
    {
      if (!File.Exists(filePath))
        throw new FileNotFoundException(string.Format("フィルター設定ファイルがありません\n{0}", filePath));
      string txt;
      using (var sr = new StreamReader(filePath, Encoding.GetEncoding(932)))
        txt = sr.ReadToEnd();
      this._filterList = txt.Replace("\r\n", "\n").Split('\n').Where(
        ln => ln.Trim().Length > 0 && ln.Trim().Substring(0, 1) != @"#").Select(ln => ln.ToLower()).ToArray();
    }

    private void setCommitComment(string workDir, StreamWriter btFlSw, SvnLogEventArgs log, string revname)
    {
      var cmmtFlNm = revname + @"_cmmnt.txt";
      var cmmt = @"以下のリポジトリよりコピー" + Environment.NewLine +
                 string.Format(@"repo:{0}", this._uri) + Environment.NewLine +
                 string.Format(@"rev :{0}", log.Revision.ToString());
      using (var st = new StreamWriter(Path.Combine(workDir, cmmtFlNm), false, Encoding.GetEncoding(932)))
        st.WriteLine(cmmt);

      var cmd = @"" + Environment.NewLine +
                string.Format(@"svn commit --file    ""{0}"" ""%dstRoot%""", Path.Combine(workDir, cmmtFlNm)) + Environment.NewLine +
                string.Format(@"svn update           ""%dstRoot%""");
      btFlSw.WriteLine(cmd);
    }

    private string retPadLeftZero(long l)
    {
      return l.ToString().PadLeft(5, '0');
    }
  }
}
