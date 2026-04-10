window.recipientInterop = {
    _instances: new Map(),

    init: function (inputElement, dotNetRef, instanceId) {
        const handler = {
            dotNetRef: dotNetRef,
            inputElement: inputElement,
            onKeyDown: function (e) {
                if (e.key === 'Enter' || e.key === 'Tab' || e.key === ',' || e.key === ' ') {
                    var val = inputElement.value.trim();
                    if (val.length > 0) {
                        e.preventDefault();
                        dotNetRef.invokeMethodAsync('JsConfirmToken', val);
                        inputElement.value = '';
                    } else if (e.key === 'Tab') {
                        // Allow normal tab when empty
                        return;
                    } else {
                        e.preventDefault();
                    }
                } else if (e.key === 'Backspace' && inputElement.value === '') {
                    e.preventDefault();
                    dotNetRef.invokeMethodAsync('JsRemoveLastToken');
                }
            },
            onPaste: function (e) {
                e.preventDefault();
                var text = (e.clipboardData || window.clipboardData).getData('text');
                if (text) {
                    dotNetRef.invokeMethodAsync('JsPasteTokens', text);
                    inputElement.value = '';
                }
            },
            onInput: function () {
                var val = inputElement.value;
                dotNetRef.invokeMethodAsync('JsInputChanged', val);
            }
        };

        inputElement.addEventListener('keydown', handler.onKeyDown);
        inputElement.addEventListener('paste', handler.onPaste);
        inputElement.addEventListener('input', handler.onInput);

        this._instances.set(instanceId, handler);
    },

    focus: function (inputElement) {
        if (inputElement) inputElement.focus();
    },

    clearInput: function (inputElement) {
        if (inputElement) inputElement.value = '';
    },

    destroy: function (instanceId) {
        var handler = this._instances.get(instanceId);
        if (handler) {
            handler.inputElement.removeEventListener('keydown', handler.onKeyDown);
            handler.inputElement.removeEventListener('paste', handler.onPaste);
            handler.inputElement.removeEventListener('input', handler.onInput);
            handler.dotNetRef.dispose();
            this._instances.delete(instanceId);
        }
    }
};
