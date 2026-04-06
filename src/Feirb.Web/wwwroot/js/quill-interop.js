window.quillInterop = {
    _instance: null,
    _container: null,

    init: function (editorElement) {
        this.destroy();
        this._container = editorElement;
        this._instance = new Quill(editorElement, {
            theme: 'snow',
            modules: {
                toolbar: [
                    [{ 'header': [1, 2, 3, false] }],
                    ['bold', 'italic', 'underline', 'strike'],
                    [{ 'list': 'ordered' }, { 'list': 'bullet' }],
                    [{ 'indent': '-1' }, { 'indent': '+1' }],
                    ['blockquote', 'code-block'],
                    ['link'],
                    [{ 'color': [] }, { 'background': [] }],
                    ['clean']
                ]
            },
            placeholder: ''
        });
    },

    destroy: function () {
        if (this._instance) {
            var toolbar = this._container
                ? this._container.previousElementSibling
                : null;
            if (toolbar && toolbar.classList.contains('ql-toolbar')) {
                toolbar.remove();
            }
            if (this._container) {
                this._container.innerHTML = '';
                this._container.className = this._container.className
                    .replace(/\bql-\S+/g, '')
                    .trim();
            }
            this._instance = null;
            this._container = null;
        }
    },

    getHtml: function () {
        if (!this._instance) return '';
        return this._instance.root.innerHTML;
    },

    setHtml: function (html) {
        if (!this._instance) return;
        var delta = this._instance.clipboard.convert({ html: html || '' });
        this._instance.setContents(delta);
    },

    getText: function () {
        if (!this._instance) return '';
        return this._instance.getText();
    },

    isEmpty: function () {
        if (!this._instance) return true;
        var text = this._instance.getText().trim();
        return text.length === 0;
    }
};

window.markedInterop = {
    toHtml: function (markdown) {
        if (typeof marked === 'undefined') return markdown;
        return marked.parse(markdown, { async: false });
    },

    toMarkdown: function (html) {
        if (typeof TurndownService === 'undefined') return html;
        var td = new TurndownService({
            headingStyle: 'atx',
            codeBlockStyle: 'fenced',
            emDelimiter: '*'
        });
        return td.turndown(html);
    }
};
