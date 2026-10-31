using HarmonyLib;
using HexDvergrAllies.Assets;
using UnityEngine;

namespace HexDvergrAllies.Core.Companions
{
    internal sealed class DvergrCompanionPinManager : MonoBehaviour
    {
        private const float UpdateInterval = 0.25f;

        private static readonly System.Reflection.FieldInfo PinUpdateRequiredField = AccessTools.Field(typeof(Minimap), "m_pinUpdateRequired");

        private Minimap.PinData _pin;
        private Minimap _minimap;
        private ZDOID _companionId = ZDOID.None;
        private float _nextUpdateTime;

        private void Update()
        {
            if (Time.time < _nextUpdateTime)
            {
                return;
            }

            _nextUpdateTime = Time.time + UpdateInterval;

            var player = Player.m_localPlayer;
            var minimap = Minimap.instance;

            if (_minimap != minimap)
            {
                _pin = null;
                _minimap = minimap;
                _companionId = ZDOID.None;
            }

            if (!player || !minimap || ZDOMan.instance == null)
            {
                RemovePin();
                return;
            }

            ZDOID companionId;

            if (!DvergrCompanionManager.TryGetHiredCompanionId(player, out companionId))
            {
                RemovePin();
                return;
            }

            if (_companionId != companionId)
            {
                RemovePin();
                _companionId = companionId;
            }

            var zdo = ZDOMan.instance.GetZDO(companionId);

            if (zdo == null)
            {
                return;
            }

            var position = zdo.GetPosition();

            if (_pin == null)
            {
                _pin = minimap.AddPin(position, Minimap.PinType.Ping, "Dvergr", false, false);
                _pin.m_icon = AssetHelper.DvergrPortrait;
                PinUpdateRequiredField.SetValue(minimap, true);
                return;
            }

            if ((_pin.m_pos - position).sqrMagnitude > 0.01f)
            {
                _pin.m_pos = position;
                PinUpdateRequiredField.SetValue(minimap, true);
            }
        }

        private void RemovePin()
        {
            if (_pin != null && _minimap != null)
            {
                _minimap.RemovePin(_pin);
            }

            _pin = null;
            _companionId = ZDOID.None;
        }

        private void OnDestroy()
        {
            RemovePin();
        }
    }
}
