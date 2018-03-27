using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using SharpSvn;
using System.IO;
using System.Net;
using AnalyzeCommandLineArgs;

namespace SvnCommitManager
{
  class Program
  {
    static string _src;
    static string _user;
    static string _psswd;
    static string _filerFl;
    static string _tmpbat;
    static long _strtRev;
    static long _endRev;
    static bool _excDel;
    static string _workdir;

    static CommandLineAnalyzer cmdAnz;

    static void Main(string[] args)
    {
      Console.WriteLine(@"抽出処理を開始します。");
      Console.WriteLine(@"");
#if DEBUG
      createAutoBatTest();
#else
      try 
      {
        setupCommandLineAnalizer();
        createAutoBat();
#endif
#if DEBUG

#else
    }
      catch (SvnClientUnrelatedResourcesException e)
      {
        Console.WriteLine(string.Format(@"エラー内容：{0}", @"指定されたリビジョンにこのリポジトリのログがありません"));
        Console.WriteLine(string.Format(@"エラークラス名：{0}", e.GetType().FullName));
        Console.WriteLine(string.Format(@"Message       ：{0}", e.Message));
      }
      catch (Exception e)
      {
        if (e.Message.Trim().Length > 0)
        {
          Console.WriteLine(string.Format(@"エラークラス名：{0}", e.GetType().FullName));
          Console.WriteLine(string.Format(@"エラー内容    ：{0}", e.Message));
        }
      }
#endif
#if DEBUG
      Console.WriteLine(@"");
      Console.WriteLine(@"出力処理が完了しました。");
      Console.WriteLine(@"終了するにはなにかキーを押してください。");
      Console.ReadKey();
#endif
    }

    static void setupCommandLineAnalizer()
    {
      cmdAnz = new CommandLineAnalyzer();
      CommandOption co_src = new CommandOption(@"-src", true, true
                                 , @"-src      [作業コピーフォルダ名]       コピー元リポジトリ作業コピーパスを指定"
                                 , (arg) => { if (!Directory.Exists(arg)) throw new Exception(@"-srcフォルダがありません"); });
      CommandOption co_user = new CommandOption(@"-user", true, true
                                 , @"-user     [-srcリポジトリのユーザ]     ログイン用ユーザ名を指定"
                                 , null);
      CommandOption co_psswd = new CommandOption(@"-psswd", true, true
                                 , @"-passwd   [-srcリポジトリのパスワード] ログイン用パスワードを指定"
                                 , null);
      CommandOption co_filerFl = new CommandOption(@"-filterFl", true, true
                                 , @"-filterFl [フィルタファイルパス]       抽出ファイルを絞り込むためのパターンを記載したファイルパス"
                                 , (arg) => { if (!File.Exists(arg)) throw new Exception(@"-filterFlファイルがありません"); });
      CommandOption co_strtRev = new CommandOption(@"-strtRev", true, true
                                 , @"-strtRev  [開始抽出リビジョン]         抽出開始のリビジョン番号を指定"
                                 , (arg) => { long.Parse(arg); });
      CommandOption co_endRev = new CommandOption(@"-endRev", true, true
                                 , @"-strtRev  [終了抽出リビジョン]         抽出終了のリビジョン番号を指定"
                                 , (arg) => { long.Parse(arg); });
      CommandOption co_tmpbat = new CommandOption(@"-tmpbat", true, true
                                 , @"-tmpbat   [テンプバッチファイルパス]   "
                                 , (arg) => { if (!File.Exists(arg)) throw new Exception(@"-tmpbatファイルがありません"); });
      CommandOption co_workDir = new CommandOption(@"-workDir", true, true
                                 , @"-workDir  [作業フォルダ名]             作業フォルダパスをフルパスで指定"
                                 , (arg) => {
                                   if (arg.IndexOf(@":") < 0) throw new Exception(@"-workDirはフルパスで指定します");
                                   if (Directory.Exists(arg)) throw new Exception(@"-workDirはすでに存在します。存在しないフォルダを指定します。処理中に自動作成します");
                                 });
      CommandOption co_excDel = new CommandOption(@"-excDel", false, false
                                 , @"-excDel                                削除分の操作を抽出しない場合、指定"
                                 , null);
      CommandOption co_Q = new CommandOption(@"/?", false, false
                                 , @"/?                                     ヘルプを表示"
                                 , null);
      cmdAnz.helpOption = co_Q;
      cmdAnz.addCommandOption(co_src);
      cmdAnz.addCommandOption(co_user);
      cmdAnz.addCommandOption(co_psswd);
      cmdAnz.addCommandOption(co_filerFl);
      cmdAnz.addCommandOption(co_strtRev);
      cmdAnz.addCommandOption(co_endRev);
      cmdAnz.addCommandOption(co_tmpbat);
      cmdAnz.addCommandOption(co_excDel);
      cmdAnz.addCommandOption(co_workDir);

      if (!cmdAnz.analyze()) throw new Exception(@"");

      _src = co_src.arg;
      _user = co_user.arg;
      _psswd = co_psswd.arg;
      _filerFl = co_filerFl.arg;
      _strtRev = long.Parse(co_strtRev.arg);
      _endRev = long.Parse(co_endRev.arg);
      if (_strtRev > _endRev)
        throw new Exception(@"-sttRevと-endRevの大小関係が不正です。-sttRevは-EndRev以下である必要があります。");
      _tmpbat = co_tmpbat.arg;
      _excDel = false;
      if (co_excDel.selected)
        _excDel = true;
      _workdir = co_workDir.arg;
    }

    static void createAutoBat()
    {
      var sc = new SvnConnector(_src, _user, _psswd);
      sc.readFilterFile(_filerFl);
      sc.createAutoCommitBat(_strtRev, _endRev, _workdir, _tmpbat, _excDel);

      sc.clearAuth();
    }
#if DEBUG
    static void createAutoBatTest()
    {
      var _wrk = @"C:\Temp\svn\testSrc\srcDir";
      _user = @"user";
      _psswd = @"admin";
      _filerFl = @"C:\Temp\svn\filter.txt";
      _strtRev = 10;
      _endRev = 31;
      _tmpbat = @"C:\Temp\svn\tmp\tmp.bat";
      _workdir = @"C:\Temp\svn\topath\20180326";
      //_excDel = false;
      _excDel = true;

      var sc = new SvnConnector(_wrk, _user, _psswd);
      sc.readFilterFile(_filerFl);
      sc.createAutoCommitBat(_strtRev,_endRev, _workdir, _tmpbat, _excDel);

      sc.clearAuth();
    }
#endif

  }
}
