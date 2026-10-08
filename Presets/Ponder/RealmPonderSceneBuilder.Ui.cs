using System;
using System.Linq;
using Engine;
using Game;

namespace RealmEX.Presets.Ponder
{
    public sealed partial class RealmPonderSceneBuilder
    {
        public RealmPonderSceneBuilder ShowUi(RealmPonderUiDefinition definition)
        { ArgumentNullException.ThrowIfNull(definition); return Schedule(s => s.Ui = new(definition)); }
        public RealmPonderSceneBuilder HideUi() => Schedule(s => s.Ui = null);
        /// <summary>Replayable edits affect only the widget tree created by ShowUi.</summary>
        public RealmPonderSceneBuilder UiEdit(Action<CanvasWidget, string> edit)
        { ArgumentNullException.ThrowIfNull(edit); return Schedule(s => Ui(s).Edit(edit)); }
        public RealmPonderSceneBuilder UiText(string target, RealmPonderText text) => UiEdit((root, language) =>
        {
            string value = text.Resolve(language);
            switch (RealmPonderUiWidget.Find(root, target))
            {
                case LabelWidget label: label.Text = value; break;
                case ButtonWidget button: button.Text = value; break;
                case TextBoxWidget box: box.Text = value; break;
                default: throw new InvalidOperationException($"UI target '{target}' does not display text.");
            }
        });
        public RealmPonderSceneBuilder UiEnabled(string target, bool enabled) => UiEdit((root, _) => RealmPonderUiWidget.Find(root, target).IsEnabled = enabled);
        public RealmPonderSceneBuilder UiVisible(string target, bool visible) => UiEdit((root, _) => RealmPonderUiWidget.Find(root, target).IsVisible = visible);
        public RealmPonderSceneBuilder UiValue(string target, float value) => UiEdit((root, _) => SetValue(root, target, value));
        public RealmPonderSceneBuilder UiInventory(string target) => UiEdit((root, _) =>
        {
            var grid = RealmPonderUiWidget.Find(root, target) as GridPanelWidget ?? throw new InvalidOperationException($"UI target '{target}' is not an inventory grid.");
            if (grid.Children.Count != 0) throw new InvalidOperationException($"UI grid '{target}' is already populated.");
            var inventory = new RealmPonderUiInventory(grid.RowsCount * grid.ColumnsCount);
            for (int i = 0; i < inventory.SlotsCount; i++)
            {
                var slot = new InventorySlotWidget { Name = target + "." + i };
                slot.AssignInventorySlot(inventory, i); grid.Children.Add(slot); grid.SetWidgetCell(slot, new(i % grid.ColumnsCount, i / grid.ColumnsCount));
            }
        });
        public RealmPonderSceneBuilder UiItem(string target, int value, int count = 1) => UiEdit((root, _) =>
        {
            var slot = RealmPonderUiWidget.Find(root, target) as InventorySlotWidget ?? throw new InvalidOperationException($"UI target '{target}' is not an inventory slot.");
            if (slot.m_inventory is not RealmPonderUiInventory inventory) throw new InvalidOperationException("Scripted item edits require an isolated tutorial inventory.");
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            inventory.RemoveSlotItems(slot.m_slotIndex, inventory.GetSlotCount(slot.m_slotIndex));
            if (count > 0) inventory.AddSlotItems(slot.m_slotIndex, value, count);
        });
        public RealmPonderSceneBuilder UiButton(string gridName, string name, RealmPonderText text, int column, int row, Vector2 size) => UiEdit((root, language) =>
        {
            var grid = RealmPonderUiWidget.Find(root, gridName) as GridPanelWidget ?? throw new InvalidOperationException($"UI target '{gridName}' is not a button grid.");
            var button = new BevelledButtonWidget { Name = name, Text = text.Resolve(language), Size = size };
            grid.Children.Add(button); grid.SetWidgetCell(button, new(column, row));
        });
        public RealmPonderSceneBuilder UiPoint(string target, int duration) => UiCue(target, null, RealmPonderUiAction.Point, duration);
        public RealmPonderSceneBuilder UiClick(string target, int duration = 20)
        {
            UiCue(target, null, RealmPonderUiAction.Click, duration);
            m_instructions.Add(new(checked(m_cursor + duration / 2), s => Ui(s).Edit((root, _) => Click(root, target))));
            return this;
        }
        public RealmPonderSceneBuilder UiDrag(string from, string target, int duration)
            => UiCue(target, from, RealmPonderUiAction.Drag, duration);
        public RealmPonderSceneBuilder UiScroll(string target, float position, int duration = 20)
        {
            UiCue(target, null, RealmPonderUiAction.Scroll, duration);
            m_instructions.Add(new(checked(m_cursor + duration / 2), s => Ui(s).Edit((root, _) =>
            {
                var scroll = RealmPonderUiWidget.Find(root, target) as ScrollPanelWidget ?? throw new InvalidOperationException($"UI target '{target}' cannot scroll.");
                scroll.ScrollPosition = position; scroll.ScrollSpeed = 0;
            })));
            return this;
        }
        public RealmPonderSceneBuilder UiAnimateValue(string target, float from, float to, int duration)
        {
            CheckDuration(duration);
            for (int tick = 0; tick <= duration; tick++)
            {
                float value = from + (to - from) * (duration == 0 ? 1 : tick / (float)duration);
                m_instructions.Add(new(checked(m_cursor + tick), s => Ui(s).Edit((root, _) => SetValue(root, target, value))));
            }
            m_end = Math.Max(m_end, checked(m_cursor + duration)); return this;
        }
        private RealmPonderSceneBuilder UiCue(string target, string from, RealmPonderUiAction action, int duration)
            => Schedule(s => Ui(s).Cue = new(target, from, action, s.Tick, duration), duration);
        private static RealmPonderUiState Ui(RealmPonderState state) => state.Ui ?? throw new InvalidOperationException("Show a UI before issuing UI instructions.");
        private static void SetValue(CanvasWidget root, string target, float value)
        {
            switch (RealmPonderUiWidget.Find(root, target))
            {
                case SliderWidget slider: slider.Value = value; break;
                case ValueBarWidget bar: bar.Value = value; break;
                default: throw new InvalidOperationException($"UI target '{target}' is not a slider or value bar.");
            }
        }
        private static void Click(CanvasWidget root, string target)
        {
            Widget widget = RealmPonderUiWidget.Find(root, target);
            if (!widget.IsEnabledGlobal || !widget.IsVisibleGlobal) return;
            var click = widget as ClickableWidget ?? (widget as ContainerWidget)?.AllChildren.OfType<ClickableWidget>().FirstOrDefault()
                ?? throw new InvalidOperationException($"UI target '{target}' is not clickable.");
            click.IsClicked = true;
            if (click.IsAutoCheckingEnabled) click.IsChecked = !click.IsChecked;
            try
            {
                // Deliver one local click to the original widget's handlers, never the player's input.
                for (Widget current = click.ParentWidget; current != null; current = current.ParentWidget)
                {
                    current.Update();
                    if (ReferenceEquals(current, root)) break;
                }
            }
            finally { click.IsClicked = false; }
        }
    }
}
