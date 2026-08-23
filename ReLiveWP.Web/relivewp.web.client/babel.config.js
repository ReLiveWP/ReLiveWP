const { browserslist } = require("./package.json");

module.exports = {
    targets: browserslist,
    presets: ["@babel/preset-env"],
};
