using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerraTechETCUtil
{
    public class UIHUDWiki : UIHUDElement
    {
		private void OnSpawn()
		{
			AddElementToGroup(ManHUD.HUDGroup.Main, UIHUD.ShowAction.Show);
			AddElementToGroup(ManHUD.HUDGroup.GamepadQuickMenuHUDElements, UIHUD.ShowAction.Show);
			AddElementToGroup(ManHUD.HUDGroup.PreventCursorTargetSelection, UIHUD.ShowAction.Show);
		}
	}
}
