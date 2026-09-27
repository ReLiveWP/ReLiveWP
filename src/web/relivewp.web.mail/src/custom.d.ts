declare module "*.svg" {
    import type { JSX } from "preact";
    const component: (props: JSX.SVGAttributes<SVGSVGElement>) => JSX.Element;
    export default component;
}
declare module "*.svg?url" {
    const content: string;
    export default content;
}
declare module "*.png" {
    const content: any;
    export default content;
}
declare module "*.css" {
    const content: any;
    export default content;
}
declare module "*.scss" {
    const content: any;
    export default content;
}
