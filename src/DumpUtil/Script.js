var _ = {
    removeElement: function (element) {
        element && element.parentNode && element.parentNode.removeChild(element);
    },
    scrollTo: function scrollTo(ele) {
        ele.scrollIntoView();
    },
    foreachSelector: function foreachSelector(sel, act) {
        var nl = document.querySelectorAll(sel);

        for (var i = 0; i < nl.length; ++i) {
            var item = nl[i];
            act(item);
        }
    }
};

function highlightId(id) {
    var reds = document.querySelectorAll('.red-border');
    for (var i = reds.length - 1; i >= 0; i--) {
        var r = reds[i];
        r.classList.remove('red-border');
    };

    var items = document.querySelectorAll('[data-id="' + id + '"]')
    if (items.length == 1) {
        var item = items[0];
        item.classList.add('red-border');
        _.scrollTo(item);
    }
}



function setupGoTo() {
    var nl = document.querySelectorAll('[data-goto]');

    for (var i = 0; i < nl.length; ++i) {
        var item = nl[i];
        console.log(item);
        item.onclick = function () {
            item = this;
            var val = item.attributes.getNamedItem('data-goto').value;
            console.log('find ' + val);
            highlightId(val);
        }
    }
}

function setupToggle() {
    _.foreachSelector('.tg', function (e) {
        var span = document.createElement('span');
        span.toggle = e;
        span.classList.add('expandtext')
        span.innerText = "...";
        span.onclick = function () {
            this.toggle.classList.toggle('h');
            _.removeElement(this);
        };
        e.parentElement.appendChild(span);
        e.classList.toggle('h');
    });
}

function main() {
    setupGoTo();
    setupToggle();
}

main();
