using Godot;
using FishEatFish.Shop;
using FishEatFish.UI.ArtifactDetailPanel;
using System.Linq;

namespace FishEatFish.UI.BackpackUI
{
    public partial class BackpackUI : Control
    {
        private GridContainer _cellsContainer;
        private PanelContainer _cellTemplate;
        private ArtifactDetailUI _detailUI;

        private const int COLUMNS_PER_ROW = 8;
        private const int CELL_SIZE = 50;
        private const int CELL_SPACING = 6;

        private int _maxCellCount = 10;
        private int _ownedArtifactCount = 0;

        public override void _Ready()
        {
            GD.Print($"[BackpackUI] _Ready called");

            _cellsContainer = GetNodeOrNull<GridContainer>("CellsContainer");
            _cellTemplate = GetNodeOrNull<PanelContainer>("CellTemplate");

            GD.Print($"[BackpackUI] Parent: {GetParent()?.Name}, Parent Path: {GetPath()}");
            GD.Print($"[BackpackUI] Trying to find ArtifactDetailUI at path: ../ArtifactDetailUI");

            _detailUI = GetNodeOrNull<ArtifactDetailUI>("../ArtifactDetailUI");
            if (_detailUI == null)
            {
                GD.PrintErr("[BackpackUI] _detailUI is null!");
                
                GD.Print($"[BackpackUI] Trying alternative path: /root/BattleScene/UI/ArtifactDetailUI");
                _detailUI = GetNodeOrNull<ArtifactDetailUI>("/root/BattleScene/UI/ArtifactDetailUI");
                if (_detailUI == null)
                {
                    GD.PrintErr("[BackpackUI] Alternative path also returned null!");
                }
                else
                {
                    GD.Print($"[BackpackUI] Found ArtifactDetailUI via alternative path!");
                }
            }
            else
            {
                GD.Print($"[BackpackUI] Found ArtifactDetailUI: {_detailUI.Name}");
            }

            if (_cellsContainer == null)
            {
                GD.PrintErr("[BackpackUI] _cellsContainer is null!");
            }

            if (_cellTemplate == null)
            {
                GD.PrintErr("[BackpackUI] _cellTemplate is null!");
            }

            UpdateMaxCellCount();
            InitializeCells();

            GD.Print($"[BackpackUI] _Ready completed");
        }

        private void UpdateMaxCellCount()
        {
            var mapManager = GetNodeOrNull<MapManager>("/root/MapManager");
            if (mapManager != null)
            {
                _maxCellCount = mapManager.MaxCreatureCount;
                GD.Print($"[BackpackUI] UpdateMaxCellCount: {_maxCellCount}");
            }
            else
            {
                _maxCellCount = 10;
                GD.Print($"[BackpackUI] UpdateMaxCellCount: using default {_maxCellCount}");
            }

            UpdateSize();
        }

        private void UpdateSize()
        {
            int rows = Mathf.CeilToInt((float)_maxCellCount / COLUMNS_PER_ROW);
            int totalWidth = COLUMNS_PER_ROW * CELL_SIZE + (COLUMNS_PER_ROW - 1) * CELL_SPACING;
            int totalHeight = rows * CELL_SIZE + Mathf.Max(0, rows - 1) * CELL_SPACING;

            CustomMinimumSize = new Vector2(totalWidth, totalHeight);
            GD.Print($"[BackpackUI] UpdateSize: rows={rows}, width={totalWidth}, height={totalHeight}");
        }

        private void InitializeCells()
        {
            if (_cellsContainer == null || _cellTemplate == null)
            {
                GD.PrintErr("[BackpackUI] InitializeCells: containers are null!");
                return;
            }

            foreach (var child in _cellsContainer.GetChildren())
            {
                child.QueueFree();
            }

            for (int i = 0; i < _maxCellCount; i++)
            {
                CreateEmptyCell(i);
            }

            GD.Print($"[BackpackUI] InitializeCells: created {_maxCellCount} cells");
        }

        private PanelContainer CreateEmptyCell(int index)
        {
            var cell = _cellTemplate.Duplicate() as PanelContainer;
            if (cell == null)
            {
                GD.PrintErr($"[BackpackUI] CreateEmptyCell: failed to duplicate cell template!");
                return null;
            }

            cell.Name = $"Cell_{index}";
            cell.Visible = true;
            cell.MouseFilter = Control.MouseFilterEnum.Stop;

            _cellsContainer.AddChild(cell);
            return cell;
        }

        public void Refresh()
        {
            GD.Print($"[BackpackUI] Refresh called");

            UpdateMaxCellCount();

            if (BlackMarkShopManager.Instance == null)
            {
                GD.PrintErr("[BackpackUI] BlackMarkShopManager.Instance is null!");
                return;
            }

            var ownedArtifacts = BlackMarkShopManager.Instance.GetOwnedArtifacts();
            _ownedArtifactCount = ownedArtifacts?.Count ?? 0;
            GD.Print($"[BackpackUI] Refresh: ownedArtifacts count={_ownedArtifactCount}");

            ClearAllCellContents();

            if (_cellsContainer == null)
            {
                GD.PrintErr("[BackpackUI] Refresh: _cellsContainer is null!");
                return;
            }

            var cells = _cellsContainer.GetChildren();
            for (int i = 0; i < cells.Count; i++)
            {
                var cell = cells[i] as PanelContainer;
                if (cell == null) continue;

                if (i < _ownedArtifactCount && ownedArtifacts != null && i < ownedArtifacts.Count)
                {
                    var artifact = ownedArtifacts[i];
                    PopulateCellWithArtifact(cell, artifact);
                }
            }

            GD.Print($"[BackpackUI] Refresh completed");
        }

        private void ClearAllCellContents()
        {
            GD.Print($"[BackpackUI] ClearAllCellContents called");
            if (_cellsContainer == null)
            {
                GD.PrintErr("[BackpackUI] ClearAllCellContents: _cellsContainer is null!");
                return;
            }

            GD.Print($"[BackpackUI] ClearAllCellContents: _cellsContainer has {_cellsContainer.GetChildCount()} children");

            foreach (var child in _cellsContainer.GetChildren())
            {
                var cell = child as PanelContainer;
                if (cell == null) continue;

                GD.Print($"[BackpackUI] ClearAllCellContents: processing cell {cell.Name} with {cell.GetChildCount()} children");

                foreach (var cellChild in cell.GetChildren().ToList())
                {
                    var childName = cellChild.Name.ToString();
                    if (!childName.StartsWith("Cell_"))
                    {
                        if (cellChild is ClickableIcon icon)
                        {
                            GD.Print($"[BackpackUI] ClearAllCellContents: removing old ClickableIcon for {icon.GetArtifactName()}");
                            icon.OnIconClicked = null;
                        }
                        else if (cellChild is ClickableCell clickableCell)
                        {
                            clickableCell.OnCellClicked = null;
                        }
                        cellChild.QueueFree();
                    }
                }
            }

            GD.Print($"[BackpackUI] ClearAllCellContents completed");
        }

        private void PopulateCellWithArtifact(PanelContainer cell, ArtifactData artifact)
        {
            if (cell == null || artifact == null)
            {
                GD.PrintErr("[BackpackUI] PopulateCellWithArtifact: cell or artifact is null!");
                return;
            }

            foreach (var cellChild in cell.GetChildren().ToList())
            {
                cellChild.QueueFree();
            }

            var clickableCell = new ClickableCell();
            clickableCell.Name = "ClickableCell";
            clickableCell.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            clickableCell.MouseFilter = MouseFilterEnum.Stop;
            clickableCell.SetArtifact(artifact);
            clickableCell.OnCellClicked += ShowArtifactDetail;

            cell.AddChild(clickableCell);

            GD.Print($"[BackpackUI] PopulateCellWithArtifact: populated cell for {artifact.name}");
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event is not InputEventMouseButton mouseEvent || !mouseEvent.Pressed || mouseEvent.ButtonIndex != MouseButton.Left)
            {
                return;
            }

            var mousePos = GetViewport().GetMousePosition();

            if (_detailUI != null && _detailUI.Visible && !_detailUI.IsPointInsideDetail(mousePos))
            {
                _detailUI.HideArtifact();
            }
        }

        private void ShowArtifactDetail(ArtifactData artifact)
        {
            GD.Print($"[BackpackUI] ShowArtifactDetail called: artifact={artifact?.name}");

            if (_detailUI == null)
            {
                GD.PrintErr("[BackpackUI] ShowArtifactDetail: _detailUI is null!");
                return;
            }

            var backpackGlobalPos = GlobalPosition;
            var backpackSize = Size;
            var detailTargetPos = new Vector2(
                backpackGlobalPos.X + backpackSize.X / 2 - 175,
                backpackGlobalPos.Y + backpackSize.Y + 10
            );

            _detailUI.GlobalPosition = detailTargetPos;
            if (_detailUI.Visible)
            {
                _detailUI.UpdateArtifact(artifact);
            }
            else
            {
                _detailUI.ShowArtifact(artifact);
            }

            GD.Print($"[BackpackUI] ShowArtifactDetail completed");
        }
    }
}
