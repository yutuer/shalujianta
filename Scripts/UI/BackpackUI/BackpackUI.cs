using Godot;
using FishEatFish.Shop;
using FishEatFish.UI.ArtifactDetailPanel;
using System.Linq;

namespace FishEatFish.UI.BackpackUI
{
    public partial class BackpackUI : PanelContainer
    {
        private HBoxContainer _cellsContainer;
        private PanelContainer _cellTemplate;
        private ArtifactDetailUI _detailUI;

        private const int MAX_COLUMNS_PER_ROW = 8;
        private const int CELL_SIZE = 50;
        private const int CELL_SPACING = 6;
        private const int ICON_SIZE = 40;

        private int _maxCellCount = 10;
        private int _ownedArtifactCount = 0;

        public override void _Ready()
        {
            GD.Print($"[BackpackUI] _Ready called");

            _cellsContainer = GetNodeOrNull<HBoxContainer>("CellsContainer");
            _cellTemplate = GetNodeOrNull<PanelContainer>("CellTemplate");

            _detailUI = GetNodeOrNull<ArtifactDetailUI>("/root/BattleScene/ArtifactDetailUI");
            if (_detailUI == null)
            {
                _detailUI = GetNodeOrNull<ArtifactDetailUI>("/root/MainMenu/ArtifactDetailUI");
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

            int totalWidth = _maxCellCount * CELL_SIZE + (_maxCellCount - 1) * CELL_SPACING;
            CustomMinimumSize = new Vector2(totalWidth, CELL_SIZE);
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
            if (_cellsContainer == null) return;

            foreach (var child in _cellsContainer.GetChildren())
            {
                var cell = child as PanelContainer;
                if (cell == null) continue;

                foreach (var cellChild in cell.GetChildren().ToList())
                {
                    var childName = cellChild.Name.ToString();
                    if (!childName.StartsWith("Cell_"))
                    {
                        cellChild.QueueFree();
                    }
                }
            }
        }

        private void PopulateCellWithArtifact(PanelContainer cell, ArtifactData artifact)
        {
            if (cell == null || artifact == null)
            {
                GD.PrintErr("[BackpackUI] PopulateCellWithArtifact: cell or artifact is null!");
                return;
            }

            foreach (var child in cell.GetChildren().ToList())
            {
                var childName = child.Name.ToString();
                if (childName != "Cell_")
                {
                    child.QueueFree();
                }
            }

            var innerPanel = new PanelContainer();
            innerPanel.Name = "InnerPanel";
            innerPanel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            innerPanel.MouseFilter = Control.MouseFilterEnum.Ignore;

            TextureRect icon = null;
            bool hasValidTexture = false;

            if (!string.IsNullOrEmpty(artifact.icon))
            {
                var texture = GD.Load<Texture2D>(artifact.icon);
                if (texture != null)
                {
                    icon = new TextureRect();
                    icon.Name = "Icon";
                    icon.CustomMinimumSize = new Vector2(ICON_SIZE, ICON_SIZE);
                    icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
                    icon.Texture = texture;
                    icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;

                    float offset = (CELL_SIZE - ICON_SIZE) / 2f;
                    icon.SetAnchorsPreset(Control.LayoutPreset.FullRect);
                    icon.SetAnchor(Side.Left, 0);
                    icon.SetAnchor(Side.Top, 0);
                    icon.SetAnchor(Side.Right, 1);
                    icon.SetAnchor(Side.Bottom, 1);
                    icon.OffsetLeft = offset;
                    icon.OffsetTop = offset;
                    icon.OffsetRight = -offset;
                    icon.OffsetBottom = -offset;

                    hasValidTexture = true;
                }
            }

            if (!hasValidTexture)
            {
                icon = new TextureRect();
                icon.Name = "Icon";
                icon.CustomMinimumSize = new Vector2(ICON_SIZE, ICON_SIZE);
                icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
                icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;

                float offset = (CELL_SIZE - ICON_SIZE) / 2f;
                icon.SetAnchorsPreset(Control.LayoutPreset.FullRect);
                icon.SetAnchor(Side.Left, 0);
                icon.SetAnchor(Side.Top, 0);
                icon.SetAnchor(Side.Right, 1);
                icon.SetAnchor(Side.Bottom, 1);
                icon.OffsetLeft = offset;
                icon.OffsetTop = offset;
                icon.OffsetRight = -offset;
                icon.OffsetBottom = -offset;

                var label = new Label();
                label.Name = "Emoji";
                label.Text = "💎";
                label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
                label.HorizontalAlignment = HorizontalAlignment.Center;
                label.VerticalAlignment = VerticalAlignment.Center;
                label.MouseFilter = Control.MouseFilterEnum.Ignore;
                icon.AddChild(label);
            }

            innerPanel.AddChild(icon);
            cell.AddChild(innerPanel);

            cell.GuiInput += (InputEvent evt) => OnCellClicked(evt, artifact);

            GD.Print($"[BackpackUI] PopulateCellWithArtifact: populated cell for {artifact.name}");
        }

        private void OnCellClicked(InputEvent evt, ArtifactData artifact)
        {
            GD.Print($"[BackpackUI] OnCellClicked called: artifact={artifact?.name}");

            if (evt is InputEventMouseButton mouseBtn)
            {
                if (mouseBtn.Pressed && mouseBtn.ButtonIndex == MouseButton.Left)
                {
                    GD.Print($"[BackpackUI] OnCellClicked: left click detected on {artifact?.name}");
                    ShowArtifactDetail(artifact);
                }
            }

            GD.Print($"[BackpackUI] OnCellClicked completed");
        }

        private void ShowArtifactDetail(ArtifactData artifact)
        {
            GD.Print($"[BackpackUI] ShowArtifactDetail called: artifact={artifact?.name}");

            if (_detailUI == null)
            {
                GD.PrintErr("[BackpackUI] ShowArtifactDetail: _detailUI is null!");
                return;
            }

            _detailUI.ShowArtifact(artifact);

            GD.Print($"[BackpackUI] ShowArtifactDetail completed");
        }
    }
}
