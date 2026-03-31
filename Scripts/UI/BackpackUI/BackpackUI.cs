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
            _cellsContainer = GetNodeOrNull<GridContainer>("CellsContainer");
            _cellTemplate = GetNodeOrNull<PanelContainer>("CellTemplate");

            _detailUI = GetNodeOrNull<ArtifactDetailUI>("../ArtifactDetailUI");
            if (_detailUI == null)
            {
                _detailUI = GetNodeOrNull<ArtifactDetailUI>("/root/BattleScene/UI/ArtifactDetailUI");
            }

            if (_detailUI == null)
            {
                var detailScene = GD.Load<PackedScene>("res://Scenes/UI/ArtifactDetailUI.tscn");
                if (detailScene != null)
                {
                    var detailNode = detailScene.Instantiate<ArtifactDetailUI>();
                    GetParent()?.AddChild(detailNode);
                    _detailUI = detailNode;
                }
            }

            UpdateMaxCellCount();
            InitializeCells();
        }

        private void UpdateMaxCellCount()
        {
            var mapManager = GetNodeOrNull<MapManager>("/root/MapManager");
            if (mapManager != null)
            {
                _maxCellCount = mapManager.MaxCreatureCount;
            }
            else
            {
                _maxCellCount = 10;
            }

            UpdateSize();
        }

        private void UpdateSize()
        {
            int rows = Mathf.CeilToInt((float)_maxCellCount / COLUMNS_PER_ROW);
            int totalWidth = COLUMNS_PER_ROW * CELL_SIZE + (COLUMNS_PER_ROW - 1) * CELL_SPACING;
            int totalHeight = rows * CELL_SIZE + Mathf.Max(0, rows - 1) * CELL_SPACING;

            CustomMinimumSize = new Vector2(totalWidth, totalHeight);
        }

        private void InitializeCells()
        {
            if (_cellsContainer == null || _cellTemplate == null)
            {
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
        }

        private PanelContainer CreateEmptyCell(int index)
        {
            var cell = _cellTemplate.Duplicate() as PanelContainer;
            if (cell == null)
            {
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
            UpdateMaxCellCount();

            if (BlackMarkShopManager.Instance == null)
            {
                return;
            }

            var ownedArtifacts = BlackMarkShopManager.Instance.GetOwnedArtifacts();
            _ownedArtifactCount = ownedArtifacts?.Count ?? 0;

            ClearAllCellContents();

            if (_cellsContainer == null)
            {
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
        }

        private void ClearAllCellContents()
        {
            if (_cellsContainer == null)
            {
                return;
            }

            foreach (var child in _cellsContainer.GetChildren())
            {
                var cell = child as PanelContainer;
                if (cell == null) continue;

                foreach (var cellChild in cell.GetChildren().ToList())
                {
                    var childName = cellChild.Name.ToString();
                    if (!childName.StartsWith("Cell_"))
                    {
                        if (cellChild is ClickableIcon icon)
                        {
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
        }

        private void PopulateCellWithArtifact(PanelContainer cell, ArtifactData artifact)
        {
            if (cell == null || artifact == null)
            {
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
            if (_detailUI == null || artifact == null)
            {
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
        }
    }
}
