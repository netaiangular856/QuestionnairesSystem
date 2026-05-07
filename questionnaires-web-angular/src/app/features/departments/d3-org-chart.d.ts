declare module 'd3-org-chart' {
  // Minimal typings — the library exposes a chainable OrgChart class.
  // We treat it as any-friendly so the consumer can call any chainable method.
  export class OrgChart<TData = unknown> {
    container(selector: HTMLElement | string): this;
    data(data: TData[] | readonly TData[]): this;
    render(): this;
    fit(): this;
    expandAll(): this;
    collapseAll(): this;
    setExpanded(id: string | number, expanded?: boolean): this;
    layout(value: 'top' | 'bottom' | 'left' | 'right'): this;
    compact(value: boolean): this;
    nodeWidth(fn: (d: unknown) => number): this;
    nodeHeight(fn: (d: unknown) => number): this;
    childrenMargin(fn: (d: unknown) => number): this;
    siblingsMargin(fn: (d: unknown) => number): this;
    neightbourMargin(fn: (a: unknown, b: unknown) => number): this;
    nodeContent(fn: (node: any, i: number, arr: any, state: any) => string): this;
    onNodeClick(fn: (id: string | number) => void): this;
    zoomIn(): this;
    zoomOut(): this;
    [key: string]: any;
  }
}
