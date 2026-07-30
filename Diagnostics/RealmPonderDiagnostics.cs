using System;
using Engine;
using Game;
using RealmEX.Presets.Ponder;

namespace RealmEX.Diagnostics
{
    /// <summary>
    /// F6 打开可见南瓜 Ponder Dialog（真实方块场景 + caption）。
    /// </summary>
    public static class RealmPonderDiagnostics
    {
        private static RealmPonderDialog m_dialog;

        public static void Start()
        {
            if (m_dialog != null)
            {
                Engine.Log.Warning("[RealmEX/Ponder] START ignored=dialog-already-open");
                return;
            }

            string stage = "start";
            try
            {
                if (GameManager.Project == null)
                {
                    throw new InvalidOperationException("Main project is not loaded.");
                }

                stage = "create-dialog";
                RealmPonderTutorial tutorial = RealmPonderSamples.CreatePumpkinTutorial();
                m_dialog = new RealmPonderDialog(tutorial);
                DialogsManager.ShowDialog(RealmPonderDialog.FindHostWidget(), m_dialog);
                Engine.Log.Information(
                    $"[RealmEX/Ponder] START tutorial={tutorial.Id} title=\"{tutorial.Title}\" mode=visible-dialog blocks=true");
            }
            catch (Exception ex)
            {
                Engine.Log.Error($"[RealmEX/Ponder] RESULT=FAIL tutorial=pumpkin stage={stage} exception={ex}");
                Cancel();
            }
        }

        public static void Update(SubsystemUpdate mainUpdate)
        {
            if (m_dialog == null)
            {
                return;
            }

            if (!ReferenceEquals(mainUpdate.Project, GameManager.Project))
            {
                return;
            }

            if (!m_dialog.IsClosed)
            {
                return;
            }

            bool completed = m_dialog.IsCompleted;
            string tutorialId = m_dialog.TutorialId;
            m_dialog = null;
            if (completed)
            {
                Engine.Log.Information(
                    $"[RealmEX/Ponder] RESULT=PASS tutorial={tutorialId} mode=visible-dialog blocks=true");
            }
            else
            {
                Engine.Log.Information(
                    $"[RealmEX/Ponder] RESULT=CLOSED tutorial={tutorialId} mode=visible-dialog completed=false");
            }
        }

        public static void Cancel()
        {
            if (m_dialog != null)
            {
                try
                {
                    m_dialog.Close();
                }
                catch (Exception ex)
                {
                    Engine.Log.Error($"[RealmEX/Ponder] CANCEL failed exception={ex}");
                }
                m_dialog = null;
            }
        }
    }
}
