interface SanitizerElementName {
    name: string;
    namespace?: string;
}

type SanitizerElement = string | SanitizerElementName;

interface SanitizerConfig {
    elements?: SanitizerElement[];
    removeElements?: SanitizerElement[];
    replaceWithChildrenElements?: SanitizerElement[];
    attributes?: SanitizerElement[];
    removeAttributes?: SanitizerElement[];
    comments?: boolean;
    dataAttributes?: boolean;
}

declare class Sanitizer {
    constructor(config?: SanitizerConfig);
    get(): SanitizerConfig;
    allowElement(element: SanitizerElement): void;
    removeElement(element: SanitizerElement): void;
    replaceElementWithChildren(element: SanitizerElement): void;
    allowAttribute(attribute: SanitizerElement): void;
    removeAttribute(attribute: SanitizerElement): void;
    setComments(allow: boolean): void;
    setDataAttributes(allow: boolean): void;
    removeUnsafe(): void;
}

interface SetHTMLOptions {
    sanitizer?: Sanitizer | SanitizerConfig;
}

interface Element {
    setHTML(html: string, options?: SetHTMLOptions): void;
}
