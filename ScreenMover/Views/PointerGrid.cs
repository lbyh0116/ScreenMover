using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
namespace ScreenMover.Views;
public sealed class PointerGrid : Grid, IDisposable {
readonly InputSystemCursor arrow=InputSystemCursor.Create(InputSystemCursorShape.Arrow);
readonly InputSystemCursor move=InputSystemCursor.Create(InputSystemCursorShape.SizeAll);
readonly InputSystemCursor forbidden=InputSystemCursor.Create(InputSystemCursorShape.UniversalNo);
bool disposed;
public void Dispose(){if(disposed)return;disposed=true;ProtectedCursor=null;arrow.Dispose();move.Dispose();forbidden.Dispose();}
public void SetDragCursor(bool dragging,bool valid) {
if(disposed)return;
ProtectedCursor=!dragging?arrow:valid?move:forbidden;
}
}
