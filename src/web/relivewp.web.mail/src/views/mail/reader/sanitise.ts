const ELEMENTS = ["img", "style"];
const ATTRIBUTES = [
    "style", "src", "alt", "width", "height", "align", "valign", "border", "bgcolor",
    "cellpadding", "cellspacing", "colspan", "rowspan", "face", "size", "background", "nowrap",
];

let sanitizer: Sanitizer | undefined;

function native(html: string): string {
    if (sanitizer === undefined) {
        sanitizer = new Sanitizer();
        for (const element of ELEMENTS) sanitizer.allowElement(element);
        for (const attribute of ATTRIBUTES) sanitizer.allowAttribute(attribute);

        sanitizer.removeUnsafe();
    }

    const holder = document.createElement("div");
    holder.setHTML(html, { sanitizer });

    return holder.innerHTML;
}

async function fallback(html: string): Promise<string> {
    const { default: DOMPurify } = await import(/* webpackChunkName: "sanitiser" */ "dompurify");
    return DOMPurify.sanitize(html, { ADD_TAGS: ["style"], FORCE_BODY: true });
}

export function sanitise(html: string): Promise<string> {
    return "setHTML" in Element.prototype
        ? Promise.resolve(native(html))
        : fallback(html);
}
