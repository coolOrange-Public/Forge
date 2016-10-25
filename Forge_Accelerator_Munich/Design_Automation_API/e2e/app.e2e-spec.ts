import { ForgeAcceleratorPage } from './app.po';

describe('forge-accelerator App', function() {
  let page: ForgeAcceleratorPage;

  beforeEach(() => {
    page = new ForgeAcceleratorPage();
  });

  it('should display message saying app works', () => {
    page.navigateTo();
    expect(page.getParagraphText()).toEqual('app works!');
  });
});
