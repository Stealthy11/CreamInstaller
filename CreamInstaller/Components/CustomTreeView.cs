﻿using CreamInstaller.Resources;
using CreamInstaller.Utility;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

using static CreamInstaller.Resources.Resources;

using TreeView = System.Windows.Forms.TreeView;

namespace CreamInstaller.Components;

internal class CustomTreeView : TreeView
{
    private Form form;
    private SolidBrush backBrush;
    private Font comboBoxFont;
    private ToolStripDropDown comboBoxDropDown;

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x203)
            m.Result = IntPtr.Zero;
        else
            base.WndProc(ref m);
        form = FindForm();
    }

    internal CustomTreeView() : base()
    {
        DrawMode = TreeViewDrawMode.OwnerDrawText;
        DrawNode += new DrawTreeNodeEventHandler(DrawTreeNode);
        TreeViewNodeSorter = PlatformIdComparer.NodeName;
        Disposed += OnDisposed;
    }

    private void OnDisposed(object sender, EventArgs e)
    {
        if (backBrush is not null)
            backBrush.Dispose();
        backBrush = null;
        if (comboBoxFont is not null)
            comboBoxFont.Dispose();
        comboBoxFont = null;
        if (comboBoxDropDown is not null)
            comboBoxDropDown.Dispose();
        comboBoxDropDown = null;
    }

    private readonly Dictionary<TreeNode, Rectangle> selectionBounds = new();
    private readonly Dictionary<ProgramSelection, Rectangle> checkBoxBounds = new();
    private readonly Dictionary<ProgramSelection, Rectangle> comboBoxBounds = new();
    private const string koaloaderToggleString = "Koaloader";

    private void DrawTreeNode(object sender, DrawTreeNodeEventArgs e)
    {
        e.DrawDefault = true;
        TreeNode node = e.Node;
        if (!node.IsVisible)
            return;

        bool highlighted = (e.State & TreeNodeStates.Selected) == TreeNodeStates.Selected && Focused;

        Graphics graphics = e.Graphics;
        backBrush ??= new(BackColor);
        Font font = node.NodeFont ?? Font;

        Brush brush = highlighted ? SystemBrushes.Highlight : backBrush;
        string text;// = e.Node.Text;
        Size size;
        Rectangle bounds = node.Bounds;
        Rectangle selectionBounds = bounds;
        Color color;// = highlighted ? SystemColors.HighlightText : (node.ForeColor != Color.Empty) ? node.ForeColor : node.TreeView.ForeColor;
        Point point;

        /*Size textSize = TextRenderer.MeasureText(text, font);
        Point textLoc = new(bounds.X - 1, bounds.Y);
        bounds = new Rectangle(textLoc, new Size(textSize.Width, bounds.Height));
        graphics.FillRectangle(brush, bounds);
        TextRenderer.DrawText(graphics, text, font, bounds, color, TextFormatFlags.Default);*/

        Form form = FindForm();
        if (form is not SelectForm and not SelectDialogForm)
            return;

        string platformId = node.Name;
        Platform platform = (node.Tag as Platform?).GetValueOrDefault(Platform.None);
        if (string.IsNullOrWhiteSpace(platformId) || platform is Platform.None)
            return;

        color = highlighted ? ColorTranslator.FromHtml("#FFFF99") : Enabled ? ColorTranslator.FromHtml("#696900") : ColorTranslator.FromHtml("#AAAA69");
        text = platform.ToString();
        size = TextRenderer.MeasureText(graphics, text, font);
        bounds = new(bounds.X + bounds.Width, bounds.Y, size.Width, bounds.Height);
        selectionBounds = new(selectionBounds.Location, selectionBounds.Size + new Size(bounds.Size.Width, 0));
        graphics.FillRectangle(brush, bounds);
        point = new(bounds.Location.X - 1, bounds.Location.Y + 1);
        TextRenderer.DrawText(graphics, text, font, point, color, TextFormatFlags.Default);

        if (platform is not Platform.Paradox)
        {
            color = highlighted ? ColorTranslator.FromHtml("#99FFFF") : Enabled ? ColorTranslator.FromHtml("#006969") : ColorTranslator.FromHtml("#69AAAA");
            text = platformId.ToString();
            size = TextRenderer.MeasureText(graphics, text, font);
            int left = -4;
            bounds = new(bounds.X + bounds.Width + left, bounds.Y, size.Width, bounds.Height);
            selectionBounds = new(selectionBounds.Location, selectionBounds.Size + new Size(bounds.Size.Width + left, 0));
            graphics.FillRectangle(brush, bounds);
            point = new(bounds.Location.X - 1, bounds.Location.Y + 1);
            TextRenderer.DrawText(graphics, text, font, point, color, TextFormatFlags.Default);
        }

        /*if (highlighted)
            ControlPaint.DrawFocusRectangle(graphics, selectionBounds, color, SystemColors.Highlight);*/

        if (form is SelectForm)
        {
            ProgramSelection selection = ProgramSelection.FromPlatformId(platform, platformId);
            if (selection is not null)
            {
                if (bounds == node.Bounds)
                {
                    size = new(4, 0);
                    bounds = new(bounds.X + bounds.Width, bounds.Y, size.Width, bounds.Height);
                    graphics.FillRectangle(brush, bounds);
                }

                CheckBoxState checkBoxState = selection.Koaloader
                    ? Enabled ? CheckBoxState.CheckedPressed : CheckBoxState.CheckedDisabled
                    : Enabled ? CheckBoxState.UncheckedPressed : CheckBoxState.UncheckedDisabled;
                size = CheckBoxRenderer.GetGlyphSize(graphics, checkBoxState);
                bounds = new(bounds.X + bounds.Width, bounds.Y, size.Width, bounds.Height);
                selectionBounds = new(selectionBounds.Location, selectionBounds.Size + new Size(bounds.Size.Width, 0));
                Rectangle checkBoxBounds = bounds;
                graphics.FillRectangle(backBrush, bounds);
                point = new(bounds.Left, bounds.Top + bounds.Height / 2 - size.Height / 2 - 1);
                CheckBoxRenderer.DrawCheckBox(graphics, point, checkBoxState);

                text = koaloaderToggleString;
                size = TextRenderer.MeasureText(graphics, text, font);
                int left = 1;
                bounds = new(bounds.X + bounds.Width, bounds.Y, size.Width + left, bounds.Height);
                selectionBounds = new(selectionBounds.Location, selectionBounds.Size + new Size(bounds.Size.Width, 0));
                checkBoxBounds = new(checkBoxBounds.Location, checkBoxBounds.Size + new Size(bounds.Size.Width, 0));
                graphics.FillRectangle(backBrush, bounds);
                point = new(bounds.Location.X - 1 + left, bounds.Location.Y + 1);
                TextRenderer.DrawText(graphics, text, font, point,
                    Enabled ? ColorTranslator.FromHtml("#006900") : ColorTranslator.FromHtml("#69AA69"),
                    TextFormatFlags.Default);

                this.checkBoxBounds[selection] = RectangleToClient(checkBoxBounds);

                if (selection.Koaloader && selection.KoaloaderProxy is not null)
                {
                    comboBoxFont ??= new(font.FontFamily, 6, font.Style, font.Unit, font.GdiCharSet, font.GdiVerticalFont);
                    ComboBoxState comboBoxState = Enabled ? ComboBoxState.Normal : ComboBoxState.Disabled;

                    text = selection.KoaloaderProxy + ".dll";
                    size = TextRenderer.MeasureText(graphics, text, comboBoxFont) + new Size(6, 0);
                    int padding = 2;
                    bounds = new(bounds.X + bounds.Width, bounds.Y + padding / 2, size.Width, bounds.Height - padding);
                    selectionBounds = new(selectionBounds.Location, selectionBounds.Size + new Size(bounds.Size.Width, 0));
                    Rectangle comboBoxBounds = bounds;
                    graphics.FillRectangle(backBrush, bounds);
                    ComboBoxRenderer.DrawTextBox(graphics, bounds, text, comboBoxFont, comboBoxState);

                    size = new(14, 0);
                    left = -1;
                    bounds = new(bounds.X + bounds.Width + left, bounds.Y, size.Width, bounds.Height);
                    selectionBounds = new(selectionBounds.Location, selectionBounds.Size + new Size(bounds.Size.Width + left, 0));
                    comboBoxBounds = new(comboBoxBounds.Location, comboBoxBounds.Size + new Size(bounds.Size.Width + left, 0));
                    ComboBoxRenderer.DrawDropDownButton(graphics, bounds, comboBoxState);

                    this.comboBoxBounds[selection] = RectangleToClient(comboBoxBounds);
                }
                else _ = comboBoxBounds.Remove(selection);
            }
        }

        this.selectionBounds[node] = RectangleToClient(selectionBounds);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Refresh();
        Point clickPoint = PointToClient(e.Location);
        SelectForm selectForm = (form ??= FindForm()) as SelectForm;
        foreach (KeyValuePair<TreeNode, Rectangle> pair in selectionBounds.ToList())
        {
            if (pair.Key.TreeView is null)
                _ = selectionBounds.Remove(pair.Key);
            else if (pair.Key.IsVisible && pair.Value.Contains(clickPoint))
            {
                SelectedNode = pair.Key;
                if (e.Button is MouseButtons.Right && selectForm is not null)
                    selectForm.OnNodeRightClick(pair.Key, e.Location);
                break;
            }
        }
        if (e.Button is MouseButtons.Left)
        {
            bool invalidate = false;
            if (comboBoxBounds.Any() && selectForm is not null)
                foreach (KeyValuePair<ProgramSelection, Rectangle> pair in comboBoxBounds.ToList())
                {
                    if (!ProgramSelection.All.Contains(pair.Key))
                        _ = comboBoxBounds.Remove(pair.Key);
                    else if (pair.Value.Contains(clickPoint))
                    {
                        List<string> proxies = EmbeddedResources.FindAll(r => r.StartsWith("Koaloader"))
                            .Select(p =>
                            {
                                p.GetProxyInfoFromIdentifier(out string proxyName, out _);
                                return proxyName;
                            }).Distinct().ToList();
                        comboBoxDropDown ??= new();
                        comboBoxDropDown.ShowItemToolTips = false;
                        comboBoxDropDown.Items.Clear();
                        foreach (string proxy in proxies)
                        {
                            bool canUse = true;
                            foreach ((string directory, BinaryType binaryType) in pair.Key.ExecutableDirectories)
                            {
                                string path = directory + @"\" + proxy + ".dll";
                                if (File.Exists(path) && !path.IsResourceFile(ResourceIdentifier.Koaloader))
                                {
                                    canUse = false;
                                    break;
                                }
                            }
                            if (canUse)
                                _ = comboBoxDropDown.Items.Add(new ToolStripButton(proxy + ".dll", null, (s, e) =>
                                {
                                    pair.Key.KoaloaderProxy = proxy;
                                    ProgramData.UpdateKoaloaderProxyChoices();
                                    Invalidate();
                                })
                                { Font = comboBoxFont });
                        }
                        comboBoxDropDown.Show(this, PointToScreen(new(pair.Value.Left, pair.Value.Bottom - 1)));
                        invalidate = true;
                        break;
                    }
                }
            if (!invalidate)
            {
                foreach (KeyValuePair<ProgramSelection, Rectangle> pair in checkBoxBounds.ToList())
                {
                    if (!ProgramSelection.All.Contains(pair.Key))
                        _ = checkBoxBounds.Remove(pair.Key);
                    else if (pair.Value.Contains(clickPoint))
                    {
                        pair.Key.Koaloader = !pair.Key.Koaloader;
                        invalidate = true;
                        break;
                    }
                }
                if (invalidate && selectForm is not null)
                {
                    CheckBox koaloaderAllCheckBox = selectForm.KoaloaderAllCheckBox();
                    koaloaderAllCheckBox.CheckedChanged -= selectForm.OnKoaloaderAllCheckBoxChanged;
                    koaloaderAllCheckBox.Checked = ProgramSelection.AllSafe.TrueForAll(selection => selection.Koaloader);
                    koaloaderAllCheckBox.CheckedChanged += selectForm.OnKoaloaderAllCheckBoxChanged;
                }
            }
            if (invalidate) Invalidate();
        }
    }
}