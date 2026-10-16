using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
namespace ScreenMover.Views;
public sealed class PointerGrid : Grid {
readonly InputSystemCursor arrow=InputSystemCursor.Create(InputSystemCursorShape.Arrow);
readonly InputSystemCursor move=InputSystemCursor.Create(InputSystemCursorShape.SizeAll);
readonly InputSystemCursor forbidden=InputSystemCursor.Create(InputSystemCursorShape.UniversalNo);
public void SetDragCursor(bool dragging,bool valid) {
ProtectedCursor=!dragging?arrow:valid?move:forbidden;
}
}
