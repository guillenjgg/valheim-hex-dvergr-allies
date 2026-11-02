using UnityEngine;

namespace HexDvergrAllies.Core.Companions
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
            return DvergrCompanionNameManager.GetCompanionName(_character);
        }

        public void SetText(string text)
        {
            DvergrCompanionNameManager.RequestRename(Player.m_localPlayer, _character, text);
        }
    }
}
