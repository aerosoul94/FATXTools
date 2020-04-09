using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Diagnostics;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FATX;

namespace FATXTools
{
    // TODO: Move to a new file
    public class CellDataEventArgs : EventArgs
    {
        public List<object> Value;
        public uint ClusterIndex;
    }

    // TODO: Move to a new file
    public enum ClusterStatus
    {
        WHITE,
        PURPLE,
        GREEN,
        YELLOW,
        ORANGE,
        RED
    }

    public partial class DataMap : Control
    {
        private int _cellCount;

        private Dictionary<uint, bool> cellSelected;
        private Dictionary<uint, Rectangle> cells;
        private Dictionary<uint, ClusterStatus> cellStatus;
        private Dictionary<uint, List<object>> cellValue;

        /// <summary>
        /// Length and height of a cell.
        /// </summary>
        private const int _cellSize = 20;

        /// <summary>
        /// Number of visible columns in the view.
        /// </summary>
        private int _visibleColumns;

        /// <summary>
        /// Number of visible rows in the view.
        /// </summary>
        private int _visibleRows;

        /// <summary>
        /// Total number of clusters.
        /// </summary>
        private long _numCells;

        /// <summary>
        /// Total number of columns.
        /// </summary>
        private long _maxColumns;

        /// <summary>
        /// Total number of rows.
        /// </summary>
        private long _maxRows;

        /// <summary>
        /// Index of the first cell in the view.
        /// </summary>
        private long _startCell;

        /// <summary>
        /// Index of the last cell in the view.
        /// </summary>
        private long _endCell;

        /// <summary>
        /// Total amount of cell visible in view.
        /// </summary>
        private long _visibleCells;

        private Font _font;

        private SolidBrush _white = new SolidBrush(Color.White);
        private SolidBrush _green = new SolidBrush(Color.Green);
        private SolidBrush _yellow = new SolidBrush(Color.Yellow);
        private SolidBrush _orange = new SolidBrush(Color.Orange);
        private SolidBrush _red = new SolidBrush(Color.Red);
        private SolidBrush _purple = new SolidBrush(Color.Purple);
        private SolidBrush _black = new SolidBrush(Color.Black);
        private SolidBrush _selectbrush = new SolidBrush(Color.Blue);

        private Rectangle _content;
        private Rectangle _grid;
        private Rectangle _offsets;
        private Rectangle _columnInfo;

        private bool _vScrollVisible;
        private long vScrollPos;
        private VScrollBar vScrollBar;
        //private HScrollBar hScrollBar;

        public event EventHandler CellSelected;

        public event EventHandler CellHovered;

        public DataMap(int cellCount)
        {
            InitializeComponent();

            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.ResizeRedraw, true);

            this._cellCount = cellCount;

            this._numCells = 0;
            this._maxColumns = 32;
            this._maxRows = this._numCells / this._maxColumns;

            this._font = new Font("Consolas", 10);

            vScrollBar = new VScrollBar();
            vScrollBar.Scroll += new ScrollEventHandler(VScrollBar_Scroll);
            vScrollBar.ValueChanged += VScrollBar_ValueChanged;
            vScrollBar.Visible = true;
            vScrollBar.Minimum = 0;
            vScrollBar.Maximum = (int)this._maxRows;
            Controls.Add(vScrollBar);
            _vScrollVisible = true;

            cells = new Dictionary<uint, Rectangle>();
            cellStatus = new Dictionary<uint, ClusterStatus>();
            cellValue = new Dictionary<uint, List<object>>();
            cellSelected = new Dictionary<uint, bool>();

            InitializeCells();
        }

        private void VScrollBar_ValueChanged(object sender, EventArgs e)
        {
            Console.WriteLine(vScrollBar.Value);
        }

        private void InitializeCells()
        {
            for (uint i = 0; i < _numCells; i++)
            {
                cellStatus[i + 1] = ClusterStatus.WHITE;
                cellValue[i + 1] = new List<object>();
                cellSelected[i + 1] = false;
            }
        }

        private void UpdateScrollBar()
        {
            vScrollBar.Minimum = 0;
            vScrollBar.Maximum = (int)_numCells;
        }

        public long NumCells
        {
            get
            {
                return this._numCells;
            }

            set
            {
                this._numCells = value;
                this._maxRows = this._numCells / this._maxColumns;
                InitializeCells();
                UpdateScrollBar();
            }
        }

        public void SetCellValue(uint index, object value)
        {
            cellValue[index].Add(value);
        }

        public void SetCellStatus(uint index, ClusterStatus status)
        {
            cellStatus[index] = status;
        }

        public List<object> GetCellValue(uint index)
        {
            return cellValue[index];
        }

        public ClusterStatus GetCellStatus(uint index)
        {
            return cellStatus[index];
        }

        private int ScalePos(long pos, int min, int max)
        {
            return (int)((pos - min) / (max - min));
        }

        private int ScrollValue(long pos)
        {
            if (vScrollBar.Maximum > Int16.MaxValue)
            {
                return (int)pos;
            }
            else
            {
                return ScalePos(pos, vScrollBar.Maximum, vScrollBar.Maximum);
            }
        }

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
                    vScrollBar.Maximum = (int)this._maxRows;
                    vScrollBar.Value = (int)vScrollPos;
                    e.NewValue = (int)vScrollPos;
                    Invalidate();
                    break;
                case ScrollEventType.LargeIncrement:
                    vScrollPos += this._visibleRows - 2;
                    vScrollBar.Minimum = 0;
                    vScrollBar.Maximum = (int)this._maxRows;
                    vScrollBar.Value = (int)vScrollPos;
                    e.NewValue = (int)vScrollPos;
                    Invalidate();
                    break;
                case ScrollEventType.ThumbPosition:
                    vScrollPos = e.NewValue;
                    vScrollBar.Minimum = 0;
                    vScrollBar.Maximum = (int)this._maxRows;
                    Invalidate();
                    break;
                case ScrollEventType.ThumbTrack:
                    vScrollPos = e.NewValue;
                    vScrollBar.Minimum = 0;
                    vScrollBar.Maximum = (int)this._maxRows;
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

        

        public KeyValuePair<uint, Rectangle>? HitTest(Point p)
        {
            if (cells != null)
            {
                foreach (var pair in cells.Where(pair => pair.Value.Contains(p)))
                {
                    return pair;
                }
            }

            return null;
        }

        private Point _lastMove;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseHover(e);

            if (e.Location == _lastMove)
            {
                return;
            }

            _lastMove = e.Location;

            var hit = HitTest(e.Location);
            if (hit != null)
            {
                CellDataEventArgs eventArgs = new CellDataEventArgs();
                eventArgs.ClusterIndex = hit.Value.Key;
                eventArgs.Value = cellValue[hit.Value.Key];
                if (CellHovered != null)
                {
                    CellHovered(this, eventArgs);
                }
            }
            else
            {
                if (CellHovered != null)
                {
                    CellHovered(this, null);
                }
            }
        }

        private void DeselectAll()
        {
            for (uint i = 0; i < _numCells; i++)
            {
                cellSelected[i + 1] = false;
            }
        }

        public void SelectCell(uint cellIndex)
        {
            cellSelected[cellIndex] = true;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            if (e.Button == MouseButtons.Left)
            {
                foreach (var pair in cells.Where(pair => pair.Value.Contains(e.Location)))
                {
                    DeselectAll();
                    SelectCell(pair.Key);
                    var dirent = cellValue[pair.Key];
                    if (dirent != null)
                    {
                        if (CellSelected != null)
                        {
                            CellDataEventArgs eventArgs = new CellDataEventArgs();
                            eventArgs.Value = cellValue[pair.Key];
                            eventArgs.ClusterIndex = pair.Key;
                            CellSelected(this, eventArgs);
                        }
                    }
                    Invalidate();
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            //      x   
            //   0 _____
            // y  |     
            //    |     

            base.OnPaint(e);

            var g = e.Graphics;
            var cellPad = 5;

            // Add extra row so there is no white space at the bottom
            this._visibleRows = (this._offsets.Height / (_cellSize + cellPad)) + 1;
            this._visibleColumns = (int)this._maxColumns;

            this._startCell = vScrollPos * this._maxColumns;
            this._endCell = this._startCell + Math.Min(this._visibleRows * this._maxColumns, this._numCells - this._startCell);
            this._visibleCells = _endCell - _startCell;

            this.cells = new Dictionary<uint, Rectangle>();

            using (Pen light = new Pen(Color.LightGray, 10))
            using (Pen black = new Pen(Color.DarkGray, 1))
            {
                //g.DrawRectangle(new Pen(Color.Black, 10), _content);
                //g.DrawRectangle(new Pen(Color.Blue, 5), ClientRectangle);

                // Draw column info
                for (int i = 0; i < this._visibleColumns; i++)
                {
                    g.DrawString((i + 1).ToString().PadLeft(2), this._font, _black, _columnInfo.X + (i * (_cellSize + cellPad)), _columnInfo.Y);
                }
                //g.DrawRectangle(light, _columnInfo);

                // Draw row info and offset
                for (int i = 0; i < this._visibleRows; i++)
                {
                    var offset = (vScrollPos + i) * (this._cellCount * this._visibleColumns);
                    var row = (vScrollPos + i) + 1;
                    g.DrawString(offset.ToString("X16"), this._font, _black, _offsets.X + 60, _offsets.Y + (i * (_cellSize + cellPad)) + 5);
                    g.DrawString(row.ToString().PadLeft(6), this._font, _black, _offsets.X, _offsets.Y + (i * (_cellSize + cellPad)) + 5);
                }
                //g.DrawRectangle(light, _offsets);

                for (int i = 0; i < _visibleCells; i++)
                {
                    int x = (i % (int)_maxColumns);  // column
                    int y = (i / (int)_maxColumns);  // row

                    var xPos = (x * (_cellSize + cellPad));
                    var yPos = (y * (_cellSize + cellPad));

                    uint cellIndex = (uint)(_startCell + (y * this._visibleColumns) + x) + 1;
                    SolidBrush brush = _white;
                    switch (cellStatus[cellIndex])
                    {
                        case ClusterStatus.PURPLE:
                            brush = _purple;
                            break;
                        case ClusterStatus.GREEN:
                            brush = _green;
                            break;
                        case ClusterStatus.YELLOW:
                            brush = _yellow;
                            break;
                        case ClusterStatus.ORANGE:
                            brush = _orange;
                            break;
                        case ClusterStatus.RED:
                            brush = _red;
                            break;
                        default:
                            break;
                    }

                    var rect = new Rectangle(_grid.X + xPos, _grid.Y + yPos, _cellSize, _cellSize);
                    cells[cellIndex] = rect;

                    if (cellSelected[cellIndex] == true)
                    {
                        var selection = new Rectangle(_grid.X + xPos - 4, _grid.Y + yPos - 4, _cellSize + 9, _cellSize + 9);
                        g.FillRectangle(_selectbrush, selection);
                    }
                    g.FillRectangle(brush, rect);
                    g.DrawRectangle(black, rect);
                }
                //g.DrawRectangle(light, _grid);
            }
        }

        private void UpdateSize()
        {
            // Something is off, because the size ends up larger than the actual size of a 16 char string
            var charSize = CreateGraphics().MeasureString("0", new Font("Consolas", 10));
            _content = ClientRectangle;
            var offsetsWidth = (int)(charSize.Width * 16);
            var columnInfoHeight = (int)Math.Ceiling(charSize.Height);
            _offsets = new Rectangle(_content.X, _content.Y + columnInfoHeight, offsetsWidth, _content.Height - columnInfoHeight);
            _grid = new Rectangle(_content.X + offsetsWidth, _content.Y + columnInfoHeight, (_cellSize + 5) * 32, _content.Height - columnInfoHeight);
            _columnInfo = new Rectangle(_content.X + offsetsWidth, _content.Y, (_cellSize + 5) * 32, columnInfoHeight);
            if (_vScrollVisible)
            {
                vScrollBar.Left = _content.X + _content.Width - vScrollBar.Width;
                vScrollBar.Top = _content.Y;
                vScrollBar.Height = _content.Height;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            UpdateSize();
        }
    }
}
