import QtQuick
import Quickshell
import qs.Ui

BarWidget {
  id: root
  moduleName: "community.quickotp"

  readonly property string binDirectory: Quickshell.env("HOME") + "/.local/bin"

  implicitWidth: button.implicitWidth
  implicitHeight: button.implicitHeight

  function launch(command) {
    Quickshell.execDetached([root.binDirectory + "/" + command])
  }

  WidgetButton {
    id: button
    bar: root.bar
    text: "󰌆"
    tooltipText: "QuickOTP — click for codes · right-click to edit"
    onPressed: function (mouseButton) {
      if (mouseButton === Qt.RightButton)
        root.launch("quickotp-editor")
      else if (mouseButton !== Qt.MiddleButton)
        root.launch("quickotp-popup")
    }
  }
}
