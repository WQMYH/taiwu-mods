using System;
using System.Collections.Generic;
using System.Linq;
using GongfaFramework.Test.Contracts;

namespace GongfaFramework.Test.Frontend;

internal interface IFrameworkView
{
    void SetStatus(string message);
}

internal sealed class FrameworkController
{
    private readonly IFrameworkView _view;
    internal FrameworkController(IFrameworkView view) => _view = view;

    internal List<GongfaRecord> Filter(string search, int sect, int type, int grade)
    {
        IEnumerable<GongfaRecord> values = FrontendRuntime.Snapshot.Records;
        if (!string.IsNullOrWhiteSpace(search))
            values = values.Where(x => x.Id.ToString().Contains(search) ||
                                       (x.Name ?? "").IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
        if (sect >= 0) values = values.Where(x => x.SectId == sect);
        if (type >= 0) values = values.Where(x => x.Type == type);
        if (grade >= 0) values = values.Where(x => x.Grade == grade);
        return values.OrderBy(x => x.SectId).ThenBy(x => x.Type).ThenBy(x => x.OrderIdInSect).ToList();
    }

    internal void Export(IEnumerable<GongfaRecord> records, string label)
    {
        try { _view.SetStatus("已导出：" + FrameworkFileService.Export(records, label)); }
        catch (Exception ex) { FrontendRuntime.Error("导出失败", ex); _view.SetStatus("导出失败：" + ex.Message); }
    }

    internal void Save(PatchDefinition patch, bool formal)
    {
        try
        {
            string path = FrameworkFileService.SavePatch(patch, formal);
            _view.SetStatus($"已保存到 {path}。{(formal ? "重启游戏后生效。" : "")}");
        }
        catch (Exception ex) { FrontendRuntime.Error("保存补丁失败", ex); _view.SetStatus("保存失败：" + ex.Message); }
    }
}
