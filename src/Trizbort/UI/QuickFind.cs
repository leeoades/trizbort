using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Trizbort.Domain.Application;
using Trizbort.Domain.Cache;
using Trizbort.Domain.Controllers;
using Trizbort.Domain.Elements;

namespace Trizbort.UI {
  public partial class QuickFind : Form {
    private readonly List<FindAutofindCacheItem> _cache;

    public QuickFind() {
      InitializeComponent();

      _cache = BuildFindCache();
      _cache = _cache.OrderBy(p => p.Text).ToList();

      var source = new AutoCompleteStringCollection();
      source.AddRange(_cache.Select(p => p.ToString()).ToArray());
      _txtFind.AutoCompleteCustomSource = source;
    }

    private void BtnCancelClick(object sender, EventArgs e) {
      Close();
    }

    private void BtnFindClick(object sender, EventArgs e) {
      DoFind();
    }

    private List<FindAutofindCacheItem> BuildFindCache() {
      var indexer = new Indexer();
      var findCacheItems = indexer.Index();

      var list = new List<FindAutofindCacheItem>();

      foreach (var item in findCacheItems) {
        var x1 = new FindAutofindCacheItem {Room = item.Element, Text = item.Name?.Trim()};
        var x2 = new FindAutofindCacheItem {Room = item.Element, Text = item.Description?.Trim()};
        var x3 = new FindAutofindCacheItem {Room = item.Element, Text = item.Objects?.Trim()};
        var x4 = new FindAutofindCacheItem {Room = item.Element, Text = item.Subtitle?.Trim()};

        list.Add(x1);
        if (!string.IsNullOrEmpty(x2.Text)) list.Add(x2);
        if (!string.IsNullOrEmpty(x3.Text)) list.Add(x3);
        if (!string.IsNullOrEmpty(x4.Text)) list.Add(x4);
      }


      return list;
    }

    private void DoFind() {
      var s = _txtFind.Text;
      if (string.IsNullOrWhiteSpace(s)) Close();

      var found = GetResults(s);

      var controller = new CanvasController();
      controller.SelectElements(found);

      Project.Current.ActiveSelectedElement = found.FirstOrDefault();

      controller.EnsureVisible(Project.Current.ActiveSelectedElement);

      Close();
    }

    private List<Element> GetResults(string s) {
      var list = _cache.Where(xx => xx.Text?.IndexOf(s, StringComparison.CurrentCultureIgnoreCase) > -1).Select(p => p.Room).ToList();
      return list;
    }

    private void QuickFind_Activated(object sender, EventArgs e) {
      _txtFind.Focus();
    }

    private void QuickFind_Deactivate(object sender, EventArgs e) {
      Close();
    }

    private void QuickFind_KeyDown(object sender, KeyEventArgs e) {
      if (e.KeyCode == Keys.Escape)
        Close();
    }

    private class FindAutofindCacheItem {
      public Element Room { get; set; }
      public string Text { get; set; }

      public override string ToString() {
        return $"{Text}";
      }
    }

    private void TxtFindKeyPress(object sender, KeyPressEventArgs e)
    {
      if (e.KeyChar == (int) Keys.Enter) DoFind();
    }
  }
}