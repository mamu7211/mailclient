window.quillInterop = {
    _instance: null,

    init: function (editorElement) {
        this.destroy();
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
            this._instance = null;
        }
    },

    getHtml: function () {
        if (!this._instance) return '';
        return this._instance.root.innerHTML;
    },

    setHtml: function (html) {
        if (!this._instance) return;
        this._instance.root.innerHTML = html;
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
        return marked.parse(markdown);
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
