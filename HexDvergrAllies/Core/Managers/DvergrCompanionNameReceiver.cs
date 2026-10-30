using UnityEngine;

namespace HexDvergrAllies.Core.Managers
{
    internal class DvergrCompanionNameReceiver : MonoBehaviour, TextReceiver
    {
        private Character _character;

        internal void Initialize(Character character)
        {
            _character = character;
        }

        public string GetText()
        {
            return DvergrCompanionManager.GetCompanionName(_character);
        }

        public void SetText(string text)
        {
            DvergrCompanionManager.RequestRename(Player.m_localPlayer, _character, text);
        }
    }
}
