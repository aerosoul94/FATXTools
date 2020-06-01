using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace FATXTools.Controls
{
    public class DataMap : Control
    {
        #region Properties

        public int CellSize { get; set; }

        public Dictionary<int, DataMapCell> Cells { get; set; }

        public long CellCount 
        { 
            get
            {
                return _cellCount;
            }
            set
            {
                InitializeCells((int)value);

                _cellCount = value;
            }
        }

        public int SelectedIndex { get; set; }

        public int Increment { get; set; } = 1;

        #endregion // Properties

        #region Internal 

        private Rectangle _content;
        private Rectangle _grid;
        private Rectangle _columnInfo;
        private Rectangle _rowInfo;

        private long _visibleColumns;
        private long _visibleRows;

        private long _cellCount;
        private long _totalColumns;
        private long _totalRows;

        private long _startCell;
        private long _endCell;
        private long _visibleCells;

        private VScrollBar vScrollBar;
        private bool vScrollVisible;
        private long vScrollPos;    // TODO: might rename to _startRow

        private Font _font = new Font("Consolas", 10);
        private Brush _fontBrush = new SolidBrush(Color.Black);

        private Pen _frameBorderPen = new Pen(Color.LightGray, 2);
        private Pen _cellBorderPen = new Pen(Color.DarkGray, 1);

        private Brush _highlightBrush = new SolidBrush(Color.Blue);

        #endregion // Internal

        #region Events

        public event EventHandler CellSelected;

        public event EventHandler CellHovered;

        #endregion  // Events

        #region Constructors

        public DataMap()
        {
            Initialize(0);
        }

        public DataMap(int cellCount)
        {
            Initialize(cellCount);
        }

        private void Initialize(int cellCount)
        {
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.ResizeRedraw, true);

            CellSize = 20;

            // TODO: Make it a property
            _totalColumns = 32;
            _totalRows = cellCount / _totalColumns;

            vScrollBar = new VScrollBar();
            vScrollBar.Scroll += VScrollBar_Scroll;
            vScrollBar.ValueChanged += VScrollBar_ValueChanged;
            vScrollBar.Visible = true;      // TODO: detect when to make it visible
            vScrollBar.Minimum = 0;
            vScrollBar.Maximum = (int)_totalRows;

            vScrollVisible = true;

            Controls.Add(vScrollBar);

            // This will call InitializeCells
            CellCount = cellCount;
        }

        private void InitializeCells(int cellCount)
        {
            Cells = new Dictionary<int, DataMapCell>(cellCount);

            for (int i = 0; i < cellCount; i++)
            {
                Cells[i] = new DataMapCell();
                Cells[i].Index = i;
            }
        }

        #endregion

        #region Control Overrides

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            // Create main frame
            _content = ClientRectangle;

            // Create sub frames
            var charSize = CreateGraphics().MeasureString("0", _font);

            var rowInfoWidth = (int)(charSize.Width * 16);
            var columnInfoHeight = (int)Math.Ceiling(charSize.Height);

            _rowInfo = new Rectangle(_content.X,
                _content.Y + columnInfoHeight,
                rowInfoWidth,
                _content.Height - columnInfoHeight);

            _columnInfo = new Rectangle(_content.X + rowInfoWidth,
                _content.Y,
                (CellSize + 5) * 32,
                columnInfoHeight);

            _grid = new Rectangle(_content.X + rowInfoWidth,
                _content.Y + columnInfoHeight,
                (CellSize + 5) * 32,
                _content.Height - columnInfoHeight);

            if (vScrollVisible)
            {
                vScrollBar.Left = _content.X + _content.Width - vScrollBar.Width;
                vScrollBar.Top = _content.Y;
                vScrollBar.Height = _content.Height;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var G = e.Graphics;
            var cellPad = 5;

            _visibleRows = (_rowInfo.Height / CellSize + cellPad) + 1;
            _visibleColumns = _totalColumns;

            _startCell = vScrollPos * _totalColumns;
            _endCell = _startCell + Math.Min(
                _visibleRows * _totalColumns,
                CellCount - _startCell);

            _visibleCells = _endCell - _startCell;

#if DEBUG
            G.DrawRectangle(_frameBorderPen, _columnInfo);
            G.DrawRectangle(_frameBorderPen, _rowInfo);
            G.DrawRectangle(_frameBorderPen, _grid);
#endif

            // Draw column info
            for (int c = 0; c < _visibleColumns; c++)
            {
                G.DrawString((c + 1).ToString().PadLeft(2),
                    _font,
                    _fontBrush,
                    _columnInfo.X + (c * (CellSize + cellPad)),
                    _columnInfo.Y);
            }

            // Draw row info
            for (int r = 0; r < _visibleRows; r++)
            {
                var offset = (vScrollPos + r) * (Increment * _visibleColumns);
                var row = (vScrollPos + r) + 1;

                // Draw row number
                G.DrawString(row.ToString().PadLeft(6),
                    _font,
                    _fontBrush,
                    _rowInfo.X,
                    _rowInfo.Y + (r * (CellSize + cellPad)) + 5);

                // Draw offset
                G.DrawString(offset.ToString("X16"),
                    _font,
                    _fontBrush,
                    _rowInfo.X + 60,
                    _rowInfo.Y + (r * (CellSize + cellPad)) + 5);
            }

            // Draw cells
            for (int i = 0; i < _visibleCells; i++)
            {
                // Get column and row for current cell
                int x = (i % (int)_totalColumns);   // column
                int y = (i / (int)_totalColumns);   // row

                // Calculate coordinates for this cell
                var xPos = (x * (CellSize + cellPad));
                var yPos = (y * (CellSize + cellPad));

                var cellIndex = (int)(_startCell + (y * _visibleColumns) + x);

                var rect = new Rectangle(_grid.X + xPos,
                    _grid.Y + yPos,
                    CellSize,
                    CellSize);

                Cells[cellIndex].Rect = rect;

                if (Cells[cellIndex].Selected)
                {
                    G.FillRectangle(_highlightBrush,
                        new Rectangle(
                            _grid.X + xPos - 4,
                            _grid.Y + yPos - 4,
                            CellSize + 9,
                            CellSize + 9));
                }

                // Draw filled rectangle
                G.FillRectangle(
                    new SolidBrush(Cells[cellIndex].Color),
                    rect);

                G.DrawRectangle(
                    _cellBorderPen,
                    rect);
            }
        }

        private DataMapCell HitTest(Point p)
        {
            foreach (var pair in Cells
                .Where(pair => pair.Value.Index >= _startCell && pair.Value.Index <= _endCell)
                //.Where(cell => cell.Rect.Contains(p)))
                .Where(pair => pair.Value.Rect.Contains(p)))
            {
                return pair.Value;
            }

            return null;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            if (e.Button == MouseButtons.Left)
            {
                var hit = HitTest(e.Location);

                if (hit != null)
                {
                    SelectedIndex = hit.Index;

                    foreach (var pair in Cells)
                    {
                        pair.Value.Selected = false;
                    }

                    hit.Selected = true;

                    CellSelected?.Invoke(this, new EventArgs());

                    Invalidate();
                }
            }
        }

        private Point _lastMove;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (e.Location == _lastMove)
            {
                return;
            }

            _lastMove = e.Location;

            var hit = HitTest(e.Location);

            if (hit != null)
            {
                CellHovered?.Invoke(this, new CellHoveredEventArgs(hit.Index));
            }
            else
            {
                CellHovered?.Invoke(this, null);
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            int numberOfRowsToMove = e.Delta * SystemInformation.MouseWheelScrollLines / 120;

            Debug.WriteLine($"Number of rows to move: {numberOfRowsToMove}");

            vScrollPos -= numberOfRowsToMove;

            if (vScrollPos > vScrollBar.Maximum)
                vScrollPos = vScrollBar.Maximum;

            if (vScrollPos < vScrollBar.Minimum)
                vScrollPos = vScrollBar.Minimum;

            vScrollBar.Value = (int)vScrollPos;

            Invalidate();

            base.OnMouseWheel(e);
        }

        #endregion

        #region Vertical Scrollbar Handling

        private void VScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            switch (e.Type)
            {
                case ScrollEventType.SmallDecrement:
                    if (vScrollPos != vScrollBar.Minimum)
                    {
                        vScrollPos--;
                        e.NewValue = (int)vScrollPos;
                        Invalidate();
                    }
                    break;
                case ScrollEventType.SmallIncrement:
                    if (vScrollPos != vScrollBar.Maximum)
                    {
                        vScrollPos++;
                        e.NewValue = (int)vScrollPos;
                        Invalidate();
                    }
                    break;
                case ScrollEventType.LargeDecrement:
                    vScrollPos -= Math.Min(vScrollPos, this._visibleRows - 2);
                    vScrollBar.Minimum = 0;
                    vScrollBar.Maximum = (int)_totalRows;
                    vScrollBar.Value = (int)vScrollPos;
                    e.NewValue = (int)vScrollPos;
                    Invalidate();
                    break;
                case ScrollEventType.LargeIncrement:
                    vScrollPos += this._visibleRows - 2;
                    vScrollBar.Minimum = 0;
                    vScrollBar.Maximum = (int)_totalRows;
                    vScrollBar.Value = (int)vScrollPos;
                    e.NewValue = (int)vScrollPos;
                    Invalidate();
                    break;
                case ScrollEventType.ThumbPosition:
                    vScrollPos = e.NewValue;
                    vScrollBar.Minimum = 0;
                    vScrollBar.Maximum = (int)_totalRows;
                    Invalidate();
                    break;
                case ScrollEventType.ThumbTrack:
                    vScrollPos = e.NewValue;
                    vScrollBar.Minimum = 0;
                    vScrollBar.Maximum = (int)_totalRows;
                    Invalidate();
                    break;
                case ScrollEventType.EndScroll:
                    break;
                case ScrollEventType.Last:
                    break;
                case ScrollEventType.First:
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void VScrollBar_ValueChanged(object sender, EventArgs e)
        {
            Debug.WriteLine(vScrollBar.Value);
        }

        #endregion
    }
}
