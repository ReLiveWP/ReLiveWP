const path = require('path');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');
const HtmlWebpackPlugin = require('html-webpack-plugin');
const TerserPlugin = require('terser-webpack-plugin');

const mode = process.env.NODE_ENV || 'production';

const babelApp = {
    loader: 'babel-loader',
    options: {
        presets: [
            ['@babel/preset-env', { useBuiltIns: 'entry', corejs: 3, bugfixes: true }],
            ['@babel/preset-react', { runtime: 'automatic', importSource: 'preact' }],
            '@babel/preset-typescript',
        ],
        cacheDirectory: true,
    },
};

const babelDeps = {
    loader: 'babel-loader',
    options: {
        babelrc: false,
        configFile: false,
        sourceType: 'unambiguous',
        presets: [['@babel/preset-env', { useBuiltIns: false }]],
        cacheDirectory: true,
    },
};

module.exports = {
    mode,
    target: ['web', 'es5'],
    devtool: 'source-map',
    entry: { login: ['./src/polyfills.ts', './src/index.tsx'] },
    output: {
        path: path.resolve(__dirname, 'dist'),
        filename: 'assets/[name].js',
        publicPath: '',
        // force ES5 output for webpack's own runtime glue
        environment: { arrowFunction: false, const: false, destructuring: false, forOf: false, templateLiteral: false, dynamicImport: false },
    },
    resolve: {
        extensions: ['.tsx', '.ts', '.js'],
        alias: {
            '~': path.resolve(__dirname, 'src/'),
            react: 'preact/compat',
            'react-dom': 'preact/compat',
        },
    },
    module: {
        rules: [
            { test: /\.tsx?$/, exclude: /node_modules/, use: [babelApp] },
            { test: /\.(js|mjs)$/, include: /node_modules/, exclude: /node_modules[\\/]core-js/, use: [babelDeps], resolve: { fullySpecified: false } },
            { test: /\.css$/i, use: [MiniCssExtractPlugin.loader, { loader: 'css-loader', options: { url: false } }] },
            { test: /\.(png|jpg|gif|svg)$/i, type: 'asset/inline' },
        ],
    },
    optimization: {
        splitChunks: false,
        runtimeChunk: false,
        minimizer: [new TerserPlugin({ terserOptions: { ecma: 5, safari10: true, format: { ecma: 5 } } })],
    },
    plugins: [
        new MiniCssExtractPlugin({ filename: 'assets/[name].css' }),
        new HtmlWebpackPlugin({ template: './src/index.html', inject: 'body', filename: 'index.html' }),
    ],
    devServer: {
        historyApiFallback: true,
        allowedHosts: 'all',
    },
};
