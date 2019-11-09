//https://github.com/maxartz15/MA_Toolbox

//References:
//https://answers.unity.com/questions/393992/custom-inspector-multi-select-enum-dropdown.html

#if UNITY_EDITOR
using UnityEngine;

namespace MA_Toolbox.Utils
{
	//Display multi-select popup for Flags enum correctly.
	public class EnumFlagAttribute : PropertyAttribute {}
}
#endif