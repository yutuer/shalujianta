using Godot;

namespace FishEatFish.UI
{
	/// <summary>
	/// 通用二次确认弹窗（参考图 R-13「确认刷新」）。
	///
	/// 用法：
	///   dialog.ShowConfirm("确认刷新", "刷新将会更新刻印和造物……");
	///   dialog.OnConfirm += () => { ...真正执行... };
	///
	/// 纯展示组件，**不持有游戏状态**；确认/取消的结果通过事件抛给调用方。
	/// </summary>
	public partial class ConfirmDialog : Control
	{
		private Label _titleLabel;
		private Label _messageLabel;
		private Button _confirmButton;
		private Button _cancelButton;

		/// <summary>点了「确认」</summary>
		public System.Action OnConfirm;

		/// <summary>点了「取消」或直接关闭</summary>
		public System.Action OnCancel;

		public override void _Ready()
		{
			GD.Print("[ConfirmDialog] _Ready called");

			_titleLabel = GetNodeOrNull<Label>("Panel/MainVBox/TitleLabel");
			_messageLabel = GetNodeOrNull<Label>("Panel/MainVBox/MessageLabel");
			_confirmButton = GetNodeOrNull<Button>("Panel/MainVBox/ButtonRow/ConfirmButton");
			_cancelButton = GetNodeOrNull<Button>("Panel/MainVBox/ButtonRow/CancelButton");

			if (_confirmButton != null)
			{
				_confirmButton.Pressed += OnConfirmPressed;
			}
			if (_cancelButton != null)
			{
				_cancelButton.Pressed += OnCancelPressed;
			}

			Visible = false;
			GD.Print("[ConfirmDialog] _Ready completed");
		}

		/// <summary>显示确认弹窗</summary>
		public void ShowConfirm(string title, string message, string confirmText = "确认", string cancelText = "取消")
		{
			if (_titleLabel == null) _titleLabel = GetNodeOrNull<Label>("Panel/MainVBox/TitleLabel");
			if (_messageLabel == null) _messageLabel = GetNodeOrNull<Label>("Panel/MainVBox/MessageLabel");
			if (_confirmButton == null) _confirmButton = GetNodeOrNull<Button>("Panel/MainVBox/ButtonRow/ConfirmButton");
			if (_cancelButton == null) _cancelButton = GetNodeOrNull<Button>("Panel/MainVBox/ButtonRow/CancelButton");

			if (_titleLabel != null) _titleLabel.Text = title ?? "";
			if (_messageLabel != null) _messageLabel.Text = message ?? "";
			if (_confirmButton != null) _confirmButton.Text = confirmText;
			if (_cancelButton != null) _cancelButton.Text = cancelText;

			Visible = true;
			GD.Print($"[ConfirmDialog] ShowConfirm: {title}");
		}

		/// <summary>关闭弹窗（不触发任何事件）</summary>
		public void Close()
		{
			Visible = false;
		}

		private void OnConfirmPressed()
		{
			GD.Print("[ConfirmDialog] 点了确认");
			Visible = false;
			OnConfirm?.Invoke();
		}

		private void OnCancelPressed()
		{
			GD.Print("[ConfirmDialog] 点了取消");
			Visible = false;
			OnCancel?.Invoke();
		}
	}
}
