function EventsTutorial(viewer, options) {
    Autodesk.Viewing.Extension.call(this, viewer, options);
    var _self = this;

    _self.load = function () {
        console.log("Extension loaded...");
        viewer.addEventListener(Autodesk.Viewing.SELECTION_CHANGED_EVENT, onSelectionChanged);
        viewer.addEventListener(Autodesk.Viewing.NAVIGATION_MODE_CHANGED_EVENT, onNavigationModeEvent);
        return true;
    };

    _self.unload = function () {
        console.log("Extension unloaded...");
        viewer.removeEventListener(Autodesk.Viewing.SELECTION_CHANGED_EVENT, onSelectionChanged);
        viewer.removeEventListener(Autodesk.Viewing.NAVIGATION_MODE_CHANGED_EVENT, onNavigationModeEvent);
        return true;
    };


// Alternative handler for Autodesk.Viewing.NAVIGATION_MODE_CHANGED_EVENT
    function onNavigationModeEvent(event) {
        var domElem = document.getElementById('MyToolValue');
        domElem.innerText = viewer.getActiveNavigationTool(); // same value as event.id
    };

    function onSelectionChanged(event) {
        var viewer = event.target;
        var currSelection = viewer.getSelection();
        var domElem = document.getElementById('MySelectionValue');
        domElem.innerText = currSelection.length;
        event.dbIdArray.forEach(function (dbId) {
            addProperties(dbId);
        });
    }

    function addProperties(dbId) {
        function _cb(result) {
            if (result.properties) {
                var table = document.createElement('table');
                for (var i = 0; i < result.properties.length; i++) {
                    addProperty(table,result.properties[i]);
                }

                var myUi = document.getElementById("myUi");
                var div = document.getElementById('props');
                if (div !== null)
                    myUi.removeChild(div);

                div = document.createElement('div');
                div.id = 'props';
                div.innerHTML = 'Properties:';
                div.appendChild(table);
                myUi.appendChild(div);
            }
        }

        viewer.getProperties(dbId, _cb);
    }

    function createPropElement(table) {

    }

    function addProperty(table, prop) {
        var row = table.insertRow(-1);
        var cell1 = row.insertCell(0);
        var cell2 = row.insertCell(1);
        cell1.innerHTML = prop.displayName;
        cell2.innerHTML = prop.displayValue;
    }
}

EventsTutorial.prototype = Object.create(Autodesk.Viewing.Extension.prototype);
EventsTutorial.prototype.constructor = EventsTutorial;
Autodesk.Viewing.theExtensionManager.registerExtension('EventsTutorial', EventsTutorial);

